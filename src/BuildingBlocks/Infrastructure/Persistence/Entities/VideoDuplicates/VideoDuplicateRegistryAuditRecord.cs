namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

public sealed class VideoDuplicateRegistryAuditRecord
{
    public Guid Id { get; set; }
    public Guid? VideoAssetId { get; set; }
    public Guid? DuplicateCandidateId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}