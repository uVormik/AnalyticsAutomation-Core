using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.FraudSignals;

public sealed class FraudSignalRecord
{
    public Guid Id { get; set; }
    public Guid UploadReceiptId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid UploaderGroupNodeId { get; set; }
    public Guid? DuplicateCandidateId { get; set; }
    public Guid? SourceVideoAssetId { get; set; }
    public Guid? MatchedVideoAssetId { get; set; }
    public string BusinessObjectKey { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int AttemptsInWindow { get; set; }
    public int DuplicateCandidatesInWindow { get; set; }
    public int Score { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}