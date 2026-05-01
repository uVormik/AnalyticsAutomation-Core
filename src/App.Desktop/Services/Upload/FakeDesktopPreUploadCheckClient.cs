using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class FakeDesktopPreUploadCheckClient(
    DesktopPreUploadCheckDecision decision = DesktopPreUploadCheckDecision.Allow) : IDesktopPreUploadCheckClient
{
    public ValueTask<DesktopPreUploadCheckResult> CheckAsync(
        DesktopPreUploadCheckRequestPreview requestPreview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopPreUploadCheckResult.FromFakeDecision(decision));
    }
}