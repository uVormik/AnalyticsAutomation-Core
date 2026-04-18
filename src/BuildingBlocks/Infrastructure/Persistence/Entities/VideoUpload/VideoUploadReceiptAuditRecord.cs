namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

public sealed class VideoUploadReceiptAuditRecord
{
    public Guid Id { get; set; }
    public Guid UploadReceiptId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}