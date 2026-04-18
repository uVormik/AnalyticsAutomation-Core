namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

public sealed class VideoUploadReceiptAnalysisJob
{
    public Guid Id { get; set; }
    public Guid UploadReceiptId { get; set; }
    public Guid PreUploadCheckId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset EnqueuedAtUtc { get; set; }
}