namespace BuildingBlocks.Infrastructure.Persistence.Entities.Audit;

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? SubjectUserId { get; set; }
    public Guid? DeviceId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}