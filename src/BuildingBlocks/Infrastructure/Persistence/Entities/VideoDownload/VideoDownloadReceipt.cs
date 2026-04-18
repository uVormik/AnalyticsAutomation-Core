using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

public sealed class VideoDownloadReceipt
{
    public Guid Id { get; set; }
    public Guid DownloadIntentId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public string ExternalVideoId { get; set; } = string.Empty;
    public string? ClientReceiptKey { get; set; }
    public long? DownloadedBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset DownloadedAtUtc { get; set; }
    public DateTimeOffset AcceptedAtUtc { get; set; }
}