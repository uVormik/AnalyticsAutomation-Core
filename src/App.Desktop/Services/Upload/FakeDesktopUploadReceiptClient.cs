using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class FakeDesktopUploadReceiptClient : IDesktopUploadReceiptClient
{
    public ValueTask<DesktopUploadReceiptResult> CreateAsync(
        DesktopUploadReceiptRequestPreview requestPreview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopUploadReceiptResult.FakeAccepted);
    }
}