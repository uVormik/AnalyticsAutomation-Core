namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

public sealed class VideoDuplicateCandidate
{
    public Guid Id { get; set; }
    public Guid SourceVideoAssetId { get; set; }
    public Guid MatchedVideoAssetId { get; set; }
    public string MatchKind { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; set; }
}