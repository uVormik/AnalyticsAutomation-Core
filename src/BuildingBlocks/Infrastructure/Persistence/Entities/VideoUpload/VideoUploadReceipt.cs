namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

public sealed class VideoUploadReceipt
{
    public Guid Id { get; set; }
    public Guid PreUploadCheckId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid? GroupNodeId { get; set; }
    public string ExternalVideoId { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string SiteStatus { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ByteSha256 { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ReceiptStatus { get; set; } = string.Empty;
    public string AnalysisJobStatus { get; set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}