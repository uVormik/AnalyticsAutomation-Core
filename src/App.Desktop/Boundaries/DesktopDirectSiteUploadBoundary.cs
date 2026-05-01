using App.Desktop.Services.Upload;

namespace App.Desktop.Boundaries;

public interface IDesktopDirectSiteUploadClient
{
    ValueTask<DesktopSiteUploadResult> UploadAsync(
        DesktopSiteUploadRequestPreview requestPreview,
        CancellationToken cancellationToken);
}