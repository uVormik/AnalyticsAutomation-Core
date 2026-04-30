namespace App.Desktop.Boundaries;

public interface IDesktopVideoFilePicker
{
    ValueTask<DesktopVideoFilePickerResult> PickVideoFileAsync(CancellationToken cancellationToken);
}

public sealed class DesktopVideoFilePickerResult
{
    private DesktopVideoFilePickerResult(
        DesktopVideoFilePickerStatus status,
        DesktopUploadSelectedFile? selectedFile,
        DesktopVideoHashSource? hashSource)
    {
        Status = status;
        SelectedFile = selectedFile;
        HashSource = hashSource;
    }

    public DesktopVideoFilePickerStatus Status { get; }

    public DesktopUploadSelectedFile? SelectedFile { get; }

    internal DesktopVideoHashSource? HashSource { get; }

    public static DesktopVideoFilePickerResult Canceled { get; } = new(
        DesktopVideoFilePickerStatus.Canceled,
        selectedFile: null,
        hashSource: null);

    public static DesktopVideoFilePickerResult Unavailable { get; } = new(
        DesktopVideoFilePickerStatus.Unavailable,
        selectedFile: null,
        hashSource: null);

    public static DesktopVideoFilePickerResult Selected(
        string fileNameOrPath,
        long sizeBytes,
        string? contentType,
        DesktopVideoHashSource? hashSource = null)
    {
        return new DesktopVideoFilePickerResult(
            DesktopVideoFilePickerStatus.Selected,
            DesktopUploadSelectedFile.FromSafeMetadata(
                fileNameOrPath,
                sizeBytes,
                contentType),
            hashSource);
    }
}

public enum DesktopVideoFilePickerStatus
{
    Canceled,
    Selected,
    Unavailable
}