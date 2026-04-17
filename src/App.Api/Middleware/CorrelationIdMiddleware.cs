using BuildingBlocks.Infrastructure.Observability;

namespace App.Api.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    IRequestContextAccessor requestContext,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incomingCorrelationId = context.Request.Headers[HeaderName].ToString();
        var correlationId = string.IsNullOrWhiteSpace(incomingCorrelationId)
            ? Guid.NewGuid().ToString("N")
            : incomingCorrelationId.Trim();

        requestContext.CorrelationId = correlationId;
        requestContext.RequestPath = context.Request.Path.Value;

        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.Value
        }))
        {
            await next(context);
        }

        requestContext.CorrelationId = null;
        requestContext.RequestPath = null;
    }
}