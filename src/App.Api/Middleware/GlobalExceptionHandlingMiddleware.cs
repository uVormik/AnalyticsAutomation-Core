using System.Diagnostics;

using BuildingBlocks.Contracts.Common;

namespace App.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogError(
                exception,
                "Unhandled exception in App.Api pipeline. TraceId={TraceId}",
                traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new ApiErrorDto(
                Code: "unhandled_error",
                Message: "Unhandled server error.",
                TraceId: traceId));
        }
    }
}