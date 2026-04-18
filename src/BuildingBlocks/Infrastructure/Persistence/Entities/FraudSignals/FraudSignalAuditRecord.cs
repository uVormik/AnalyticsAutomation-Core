using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

public sealed class FraudSignalAuditRecord
{
    public Guid Id { get; set; }
    public Guid? SignalId { get; set; }
    public Guid? IncidentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}