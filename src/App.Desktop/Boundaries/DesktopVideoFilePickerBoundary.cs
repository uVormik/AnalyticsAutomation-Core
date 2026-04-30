namespace App.Desktop.Boundaries;

public interface IDesktopVideoFilePicker
{
    ValueTask<DesktopVideoFilePickerResult> PickVideoFileAsync(CancellationToken cancellationToken);
}

public sealed class DesktopVideoFilePickerResult
{
    private DesktopVideoFilePickerResult(
        DesktopVideoFilePickerStatus status,
        DesktopUploadSelectedFile? selectedFile)
    {
        Status = status;
        SelectedFile = selectedFile;
    }

    public DesktopVideoFilePickerStatus Status { get; }

    public DesktopUploadSelectedFile? SelectedFile { get; }

    public static DesktopVideoFilePickerResult Canceled { get; } = new(
        DesktopVideoFilePickerStatus.Canceled,
        selectedFile: null);

    public static DesktopVideoFilePickerResult Unavailable { get; } = new(
        DesktopVideoFilePickerStatus.Unavailable,
        selectedFile: null);

    public static DesktopVideoFilePickerResult Selected(
        string fileNameOrPath,
        long sizeBytes,
        string? contentType)
    {
        return new DesktopVideoFilePickerResult(
            DesktopVideoFilePickerStatus.Selected,
            DesktopUploadSelectedFile.FromSafeMetadata(
                fileNameOrPath,
                sizeBytes,
                contentType));
    }
}

public enum DesktopVideoFilePickerStatus
{
    Canceled,
    Selected,
    Unavailable
}