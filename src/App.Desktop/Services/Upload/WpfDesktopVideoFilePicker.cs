using System.IO;

using App.Desktop.Boundaries;

using Microsoft.Win32;

namespace App.Desktop.Services.Upload;

public sealed class WpfDesktopVideoFilePicker : IDesktopVideoFilePicker
{
    private const string VideoFilter =
        "Video files (*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.webm)|*.mp4;*.mov;*.m4v;*.avi;*.mkv;*.webm|All files (*.*)|*.*";

    public ValueTask<DesktopVideoFilePickerResult> PickVideoFileAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dialog = new OpenFileDialog
        {
            Title = DesktopUploadSectionText.SelectVideoFileButton,
            Filter = VideoFilter,
            Multiselect = false,
            CheckFileExists = true,
            CheckPathExists = true,
            AddExtension = false
        };

        bool? accepted = dialog.ShowDialog();

        cancellationToken.ThrowIfCancellationRequested();

        if (accepted != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return ValueTask.FromResult(DesktopVideoFilePickerResult.Canceled);
        }

        return ValueTask.FromResult(CreateSafeSelection(dialog.FileName));
    }

    private static DesktopVideoFilePickerResult CreateSafeSelection(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return DesktopVideoFilePickerResult.Selected(
                fileInfo.Name,
                fileInfo.Length,
                ResolveContentType(fileInfo.Extension),
                DesktopVideoHashSource.FromLocalFilePath(fileInfo.FullName));
        }
        catch (Exception exception) when (IsSafeMetadataFailure(exception))
        {
            return DesktopVideoFilePickerResult.Unavailable;
        }
    }

    private static string? ResolveContentType(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".m4v" => "video/x-m4v",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            _ => null
        };
    }

    private static bool IsSafeMetadataFailure(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException;
    }
}