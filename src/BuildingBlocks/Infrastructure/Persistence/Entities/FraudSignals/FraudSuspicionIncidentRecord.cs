using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

public sealed class FraudSuspicionIncidentRecord
{
    public Guid Id { get; set; }
    public Guid SignalId { get; set; }
    public Guid UploadReceiptId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid UploaderGroupNodeId { get; set; }
    public Guid? DuplicateCandidateId { get; set; }
    public string BusinessObjectKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Score { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public bool EscalatedHigher { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}