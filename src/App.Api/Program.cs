using System.Linq;
using System.Runtime.InteropServices;

using App.Api.Middleware;

using BuildingBlocks.Contracts.Auth;
using BuildingBlocks.Infrastructure.AssemblyMetadata;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Modules.Auth;
using Modules.Auth.Services;

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
builder.Services.AddPlatformPersistence(databaseOptions);
builder.Services.AddAuthModule(builder.Configuration, builder.Environment);

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("App.Api self-check passed."),
        tags: new[] { "ready" })
    .AddDbContextCheck<PlatformDbContext>(
        name: "postgresql",
        tags: new[] { "ready" });

var app = builder.Build();

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
        framework = RuntimeInformation.FrameworkDescription,
        assemblyVersion = typeof(Program).Assembly.GetName().Version?.ToString(),
        databaseProvider = "PostgreSQL / EF Core / Npgsql",
        authMode = "Opaque access token + server side refresh session",
        startedAtUtc
    }));

app.MapPost(
    "/api/auth/sign-in",
    async Task<IResult> (
        SignInRequestDto request,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var response = await authService.SignInAsync(request, cancellationToken);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
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

app.Logger.LogInformation(
    "App.Api started. Environment={Environment} StartedAtUtc={StartedAtUtc} Database={Database}",
    app.Environment.EnvironmentName,
    startedAtUtc,
    databaseOptions.Database);

app.Run();