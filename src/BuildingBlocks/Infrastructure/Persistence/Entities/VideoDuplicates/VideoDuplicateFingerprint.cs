namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

public sealed class VideoDuplicateFingerprint
{
    public Guid Id { get; set; }
    public Guid VideoAssetId { get; set; }
    public string FingerprintKind { get; set; } = string.Empty;
    public string FingerprintValue { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}