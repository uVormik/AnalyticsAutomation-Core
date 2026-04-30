using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class FakeDesktopVideoFilePicker : IDesktopVideoFilePicker
{
    public ValueTask<DesktopVideoFilePickerResult> PickVideoFileAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopVideoFilePickerResult.Selected(
            DesktopUploadSelectedFile.VisualSmokeFileName,
            DesktopUploadSelectedFile.VisualSmokeFileSizeBytes,
            DesktopUploadSelectedFile.VisualSmokeContentType,
            DesktopVideoHashSource.VisualSmoke));
    }
}