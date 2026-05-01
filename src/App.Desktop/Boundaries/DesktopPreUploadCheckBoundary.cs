using App.Desktop.Services.Upload;

namespace App.Desktop.Boundaries;

public interface IDesktopPreUploadCheckClient
{
    ValueTask<DesktopPreUploadCheckResult> CheckAsync(
        DesktopPreUploadCheckRequestPreview requestPreview,
        CancellationToken cancellationToken);
}