using System;

namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

public sealed class VideoDownloadIntent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid GroupNodeId { get; set; }
    public Guid VideoAssetId { get; set; }
    public string ExternalVideoId { get; set; } = string.Empty;
    public string BusinessObjectKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SiteProvider { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public string? RejectedReason { get; set; }
}