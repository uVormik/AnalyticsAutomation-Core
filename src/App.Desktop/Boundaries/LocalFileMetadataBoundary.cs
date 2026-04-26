namespace App.Desktop.Boundaries;

public interface ILocalFileMetadataService
{
    ValueTask<LocalFileMetadata> ReadSha256MetadataAsync(
        DesktopPickedFile file,
        CancellationToken cancellationToken);
}

public sealed record LocalFileMetadata(
    string FilePath,
    long SizeBytes,
    string Sha256);