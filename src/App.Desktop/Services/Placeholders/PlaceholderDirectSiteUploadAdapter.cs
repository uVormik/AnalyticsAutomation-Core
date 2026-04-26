using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderDirectSiteUploadAdapter : IDirectSiteUploadAdapter
{
    public ValueTask<DirectSiteUploadResult> UploadAsync(
        DirectSiteUploadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new DirectSiteUploadResult(
            DirectSiteUploadStatus.Deferred,
            "S2-48 skeleton only; no direct site upload is made.",
            SiteReference: null));
    }
}