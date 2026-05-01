using App.Desktop.Services.Upload;

namespace App.Desktop.Boundaries;

public interface IDesktopUploadReceiptClient
{
    ValueTask<DesktopUploadReceiptResult> CreateAsync(
        DesktopUploadReceiptRequestPreview requestPreview,
        CancellationToken cancellationToken);
}