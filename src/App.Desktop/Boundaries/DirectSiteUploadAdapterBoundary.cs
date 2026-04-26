namespace App.Desktop.Boundaries;

public interface IDirectSiteUploadAdapter
{
    ValueTask<DirectSiteUploadResult> UploadAsync(
        DirectSiteUploadRequest request,
        CancellationToken cancellationToken);
}

public sealed record DirectSiteUploadRequest(
    string LocalFilePath,
    Uri SiteUploadEndpoint);

public sealed record DirectSiteUploadResult(
    DirectSiteUploadStatus Status,
    string Reason,
    string? SiteReference);

public enum DirectSiteUploadStatus
{
    Deferred
}