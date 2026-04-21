namespace App.Web.Features.Upload.Models;

public sealed class UploadPreCheckFormModel
{
    public string UserId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string GroupNodeId { get; set; } = string.Empty;

    public string BusinessObjectKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string ByteSha256 { get; set; } = string.Empty;

    public string ContentType { get; set; } = "video/mp4";

    public string CapturedAtUtc { get; set; } = "2026-04-20T12:00:00Z";
}