namespace App.Web.Features.Upload.Models;

public sealed class UploadReceiptFormModel
{
    public string PreUploadCheckId { get; set; } = string.Empty;

    public string ClientReceiptKey { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string GroupNodeId { get; set; } = string.Empty;

    public string BusinessObjectKey { get; set; } = string.Empty;

    public string ExternalVideoId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "video/mp4";

    public string StorageKey { get; set; } = string.Empty;

    public string SiteStatus { get; set; } = "uploaded";

    public long SizeBytes { get; set; }

    public string ByteSha256 { get; set; } = string.Empty;

    public string UploadedAtUtc { get; set; } = "2026-04-20T12:00:00Z";
}