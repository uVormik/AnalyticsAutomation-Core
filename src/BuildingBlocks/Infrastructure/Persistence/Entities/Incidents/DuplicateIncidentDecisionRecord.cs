using System;
namespace BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

public sealed class DuplicateIncidentDecisionRecord
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Guid DecidedByUserId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
}