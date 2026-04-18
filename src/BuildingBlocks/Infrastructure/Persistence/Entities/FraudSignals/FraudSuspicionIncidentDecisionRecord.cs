using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

public sealed class FraudSuspicionIncidentDecisionRecord
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Guid DecidedByUserId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
}