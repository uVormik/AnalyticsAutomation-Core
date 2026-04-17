using System.Linq;
using System.Runtime.InteropServices;

using App.Api.Middleware;

using BuildingBlocks.Infrastructure.AssemblyMetadata;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
        startedAtUtc
    }));

app.Logger.LogInformation(
    "App.Api started. Environment={Environment} StartedAtUtc={StartedAtUtc} Database={Database}",
    app.Environment.EnvironmentName,
    startedAtUtc,
    databaseOptions.Database);

app.Run();