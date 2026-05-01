using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DisabledDesktopPreUploadCheckClient : IDesktopPreUploadCheckClient
{
    public ValueTask<DesktopPreUploadCheckResult> CheckAsync(
        DesktopPreUploadCheckRequestPreview requestPreview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopPreUploadCheckResult.Deferred);
    }
}