using System;
namespace BuildingBlocks.Infrastructure.Persistence.Entities.Incidents;

public sealed class DuplicateIncidentRecord
{
    public Guid Id { get; set; }
    public Guid DuplicateCandidateId { get; set; }
    public Guid SourceVideoAssetId { get; set; }
    public Guid MatchedVideoAssetId { get; set; }
    public Guid UploaderUserId { get; set; }
    public Guid UploaderGroupNodeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string MatchKind { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public bool EscalatedHigher { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}