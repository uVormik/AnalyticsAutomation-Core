namespace App.Web.Features.Upload.SiteGateway;

public sealed record DirectSiteVideoUploadResult(
    bool IsConfigured,
    bool Succeeded,
    string? ExternalVideoId,
    string? StorageKey,
    string? SiteStatus,
    string Message)
{
    public static DirectSiteVideoUploadResult NotConfigured(string message) =>
        new(
            IsConfigured: false,
            Succeeded: false,
            ExternalVideoId: null,
            StorageKey: null,
            SiteStatus: null,
            Message: message);
}