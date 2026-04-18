using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

public sealed class VideoDownloadAuditRecord
{
    public Guid Id { get; set; }
    public Guid? DownloadIntentId { get; set; }
    public Guid? DownloadReceiptId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}