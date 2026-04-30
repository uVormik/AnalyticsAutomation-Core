using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class FakeDesktopVideoHashService : IDesktopVideoHashService
{
    public const string VisualSmokeSha256Hex =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public ValueTask<DesktopVideoHashResult> CalculateSha256Async(
        DesktopVideoHashRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopVideoHashResult.Succeeded(VisualSmokeSha256Hex));
    }
}