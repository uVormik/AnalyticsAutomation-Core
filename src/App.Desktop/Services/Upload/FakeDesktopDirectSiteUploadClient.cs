using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class FakeDesktopDirectSiteUploadClient : IDesktopDirectSiteUploadClient
{
    public ValueTask<DesktopSiteUploadResult> UploadAsync(
        DesktopSiteUploadRequestPreview requestPreview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopSiteUploadResult.FakeSuccess);
    }
}