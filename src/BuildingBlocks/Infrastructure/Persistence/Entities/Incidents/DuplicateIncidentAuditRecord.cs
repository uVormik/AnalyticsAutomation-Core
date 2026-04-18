using System;
namespace BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

public sealed class DuplicateIncidentAuditRecord
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}