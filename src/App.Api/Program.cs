using System.Linq;

using App.Api.Middleware;

using BuildingBlocks.Contracts.Auth;
using BuildingBlocks.Contracts.Devices;
using BuildingBlocks.Infrastructure.AssemblyMetadata;
using BuildingBlocks.Infrastructure.Observability;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.PlatformRuntime;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Modules.Auth;
using Modules.Auth.Services;
using Modules.Devices;
using Modules.GroupTree;
using Modules.WebsiteIntegration;

var startedAtUtc = DateTimeOffset.UtcNow;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
});

var databaseOptions = new DatabaseOptions
{
    ConnectionString = builder.Configuration.GetConnectionString("MainDatabase"),
    Host = builder.Configuration["Database:Host"] ?? DatabaseOptions.DefaultHost,
    Port = int.TryParse(builder.Configuration["Database:Port"], out var databasePort)
        ? databasePort
        : DatabaseOptions.DefaultPort,
    Database = builder.Configuration["Database:Database"] ?? DatabaseOptions.DefaultDatabase,
    Username = builder.Configuration["Database:Username"] ?? DatabaseOptions.DefaultUsername,
    PasswordFilePath = builder.Configuration["Database:PasswordFilePath"]
};

builder.Services.AddProblemDetails();
builder.Services.AddPlatformRuntimeFoundation(builder.Configuration);
builder.Services.AddPlatformPersistence(databaseOptions);
builder.Services.AddAuditObservabilityFoundation();
builder.Services.AddAuthModule(builder.Configuration, builder.Environment);
builder.Services.AddGroupTreeModule(builder.Configuration, builder.Environment);
builder.Services.AddDevicesModule(builder.Configuration);
builder.Services.AddWebsiteIntegrationModule(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("App.Api self-check passed."),
        tags: new[] { "ready" })
    .AddCheck(
        "platform_runtime",
        () => HealthCheckResult.Healthy("Feature flags and modular options foundation ready."),
        tags: new[] { "ready" })
    .AddCheck(
        "audit_foundation",
        () => HealthCheckResult.Healthy("Audit service baseline ready."),
        tags: new[] { "ready" })
    .AddCheck(
        "site_gateway",
        () => HealthCheckResult.Healthy("Website integration stub gateway ready."),
        tags: new[] { "ready" })
    .AddDbContextCheck<PlatformDbContext>(
        name: "postgresql",
        tags: new[] { "ready" });

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapGet(
    "/",
    () => Results.Ok(new
    {
        service = "App.Api",
        status = "ok"
    }));

app.MapGet(
    "/health/live",
    (HttpContext context) => Results.Ok(new
    {
        status = "live",
        service = "App.Api",
        traceId = context.TraceIdentifier
    }));

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString().ToLowerInvariant(),
                    description = entry.Value.Description
                })
            });
        }
    });

app.MapGet(
    "/api/system/version",
    (IHostEnvironment environment) => Results.Ok(new
    {
        service = environment.ApplicationName,
        environmentName = environment.EnvironmentName,
        version = ApplicationVersionReader.GetVersion<Program>(),
        framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
        databaseProvider = "PostgreSQL / EF Core / Npgsql",
        startedAtUtc
    }));

app.MapGet(
    "/api/system/platform-foundation",
    (IFeatureFlagService featureFlags) => Results.Ok(new
    {
        featureFlags = new
        {
            authDevelopmentBootstrapEnabled = featureFlags.IsEnabled(PlatformFeatureFlags.AuthDevelopmentBootstrapEnabled),
            devicesDeviceRegistrationEnabled = featureFlags.IsEnabled(PlatformFeatureFlags.DevicesDeviceRegistrationEnabled)
        },
        optionsValidation = "ValidateOnStart",
        internalEvents = new[]
        {
            "DeviceRegistrationUpsertedInternalEvent"
        }
    }));

app.MapGet(
    "/api/system/observability",
    (IRequestContextAccessor requestContext) => Results.Ok(new
    {
        correlationId = requestContext.CorrelationId,
        healthChecks = new[]
        {
            "self",
            "platform_runtime",
            "audit_foundation",
            "postgresql"
        },
        auditCategories = AuditCategories.All
    }));

app.MapPost(
    "/api/auth/sign-in",
    async Task<IResult> (
        SignInRequestDto request,
        IAuthService authService,
        IAuditService auditService,
        CancellationToken cancellationToken) =>
    {
        var response = await authService.SignInAsync(request, cancellationToken);

        if (response is null)
        {
            await auditService.WriteAsync(
                new AuditWriteEntry(
                    AuditCategories.Authentication,
                    "sign_in_failed",
                    null,
                    request.DeviceId,
                    "auth_session",
                    null,
                    new
                    {
                        request.Login
                    }),
                cancellationToken);

            return Results.Unauthorized();
        }

        await auditService.WriteAsync(
            new AuditWriteEntry(
                AuditCategories.Authentication,
                "sign_in_succeeded",
                response.User.UserId,
                response.Session.DeviceId,
                "auth_session",
                response.Session.SessionId.ToString(),
                new
                {
                    response.User.UserName,
                    response.User.Roles
                }),
            cancellationToken);

        return Results.Ok(response);
    });

app.MapPost(
    "/api/auth/refresh",
    async Task<IResult> (
        RefreshSessionRequestDto request,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    });

app.MapGet(
    "/api/group-tree/nodes",
    async Task<IResult> (
        IGroupTreeQueryService service,
        CancellationToken cancellationToken) =>
    {
        var result = await service.GetNodesAsync(cancellationToken);
        return Results.Ok(result);
    });

app.MapGet(
    "/api/group-tree/routing-preview",
    async Task<IResult> (
        Guid groupNodeId,
        Guid uploaderUserId,
        IGroupTreeQueryService service,
        CancellationToken cancellationToken) =>
    {
        var result = await service.PreviewRoutingAsync(groupNodeId, uploaderUserId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });

app.MapPost(
    "/api/devices/register",
    async Task<IResult> (
        DeviceRegistrationUpsertRequestDto request,
        IDeviceRegistrationService service,
        IAuditService auditService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await service.UpsertAsync(request, cancellationToken);

            await auditService.WriteAsync(
                new AuditWriteEntry(
                    AuditCategories.Devices,
                    "device_registration_upserted",
                    request.LastKnownUserId,
                    request.DeviceId,
                    "device_registration",
                    result.DeviceId.ToString(),
                    new
                    {
                        result.Platform,
                        result.DeviceName,
                        result.IsTrusted
                    }),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (FeatureDisabledException ex)
        {
            await auditService.WriteAsync(
                new AuditWriteEntry(
                    AuditCategories.Devices,
                    "device_registration_blocked_by_flag",
                    request.LastKnownUserId,
                    request.DeviceId,
                    "device_registration",
                    request.DeviceId.ToString(),
                    new
                    {
                        ex.FlagName,
                        request.Platform
                    }),
                cancellationToken);

            return Results.Conflict(new
            {
                error = "feature_disabled",
                featureFlag = ex.FlagName
            });
        }
    });

app.MapWebsiteIntegrationEndpoints();

app.Logger.LogInformation(
    "App.Api started. Environment={Environment} StartedAtUtc={StartedAtUtc} Database={Database}",
    app.Environment.EnvironmentName,
    startedAtUtc,
    databaseOptions.Database);

app.Run();