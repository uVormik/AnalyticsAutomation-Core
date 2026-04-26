namespace App.Desktop.Boundaries;

public interface IDesktopFilePicker
{
    ValueTask<IReadOnlyList<DesktopPickedFile>> PickVideoFilesAsync(CancellationToken cancellationToken);
}

public sealed record DesktopPickedFile(
    string FilePath,
    long SizeBytes);