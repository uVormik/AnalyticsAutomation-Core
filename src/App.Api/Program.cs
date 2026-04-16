using System.Linq;
using System.Runtime.InteropServices;

using App.Api.Middleware;

using BuildingBlocks.Infrastructure.AssemblyMetadata;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var startedAtUtc = DateTimeOffset.UtcNow;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("App.Api self-check passed."),
        tags: ["ready"]);

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
        startedAtUtc
    }));

app.Logger.LogInformation(
    "App.Api started. Environment={Environment} StartedAtUtc={StartedAtUtc}",
    app.Environment.EnvironmentName,
    startedAtUtc);

app.Run();