using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

public sealed class FraudSuspicionIncidentAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Guid AssignedAdminUserId { get; set; }
    public Guid AssignmentGroupNodeId { get; set; }
    public string AssignmentReason { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
}