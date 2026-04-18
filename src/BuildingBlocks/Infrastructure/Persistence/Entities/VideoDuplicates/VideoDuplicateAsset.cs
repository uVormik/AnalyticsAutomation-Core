namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

public sealed class VideoDuplicateAsset
{
    public Guid Id { get; set; }
    public Guid UploadReceiptId { get; set; }
    public Guid PreUploadCheckId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid? GroupNodeId { get; set; }
    public string BusinessObjectKey { get; set; } = string.Empty;
    public string ExternalVideoId { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ByteSha256 { get; set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; set; }
    public DateTimeOffset RegisteredAtUtc { get; set; }
    public bool IsActive { get; set; }
}