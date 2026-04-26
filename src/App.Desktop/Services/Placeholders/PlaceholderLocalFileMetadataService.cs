using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderLocalFileMetadataService : ILocalFileMetadataService
{
    public ValueTask<LocalFileMetadata> ReadSha256MetadataAsync(
        DesktopPickedFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();

        throw new NotSupportedException(
            "S2-48 defines the local file metadata/SHA-256 boundary only; hashing is deferred.");
    }
}