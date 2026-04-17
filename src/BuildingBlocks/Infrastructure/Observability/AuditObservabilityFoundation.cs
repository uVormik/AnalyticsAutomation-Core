using System.Text.Json;

using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Observability;

public static class AuditCategories
{
    public const string Authentication = "authentication";
    public const string Devices = "devices";
    public const string GroupTree = "group_tree";

    public static readonly IReadOnlyCollection<string> All =
    [
        Authentication,
        Devices,
        GroupTree
    ];
}

public sealed record AuditWriteEntry(
    string Category,
    string Action,
    Guid? SubjectUserId,
    Guid? DeviceId,
    string? EntityType,
    string? EntityId,
    object? Payload);

public interface IRequestContextAccessor
{
    string? CorrelationId { get; set; }
    string? RequestPath { get; set; }
}

internal sealed class RequestContextState
{
    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
}

internal sealed class AsyncLocalRequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<RequestContextState?> Current = new();

    public string? CorrelationId
    {
        get => Current.Value?.CorrelationId;
        set
        {
            var state = Current.Value ??= new RequestContextState();
            state.CorrelationId = value;
        }
    }

    public string? RequestPath
    {
        get => Current.Value?.RequestPath;
        set
        {
            var state = Current.Value ??= new RequestContextState();
            state.RequestPath = value;
        }
    }
}

public interface IAuditService
{
    Task WriteAsync(AuditWriteEntry entry, CancellationToken cancellationToken);
}

internal sealed class DatabaseAuditService(
    PlatformDbContext dbContext,
    IRequestContextAccessor requestContext,
    ILogger<DatabaseAuditService> logger) : IAuditService
{
    public async Task WriteAsync(AuditWriteEntry entry, CancellationToken cancellationToken)
    {
        var record = new AuditRecord
        {
            Id = Guid.NewGuid(),
            Category = entry.Category,
            Action = entry.Action,
            SubjectUserId = entry.SubjectUserId,
            DeviceId = entry.DeviceId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            CorrelationId = requestContext.CorrelationId,
            RequestPath = requestContext.RequestPath,
            PayloadJson = entry.Payload is null ? null : JsonSerializer.Serialize(entry.Payload),
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.AuditRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Audit record stored. Category={Category} Action={Action} CorrelationId={CorrelationId} EntityType={EntityType} EntityId={EntityId}",
            record.Category,
            record.Action,
            record.CorrelationId,
            record.EntityType,
            record.EntityId);
    }
}

public static class AuditObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddAuditObservabilityFoundation(this IServiceCollection services)
    {
        services.AddSingleton<IRequestContextAccessor, AsyncLocalRequestContextAccessor>();
        services.AddScoped<IAuditService, DatabaseAuditService>();

        return services;
    }
}