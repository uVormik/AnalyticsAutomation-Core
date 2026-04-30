using System.IO;
using System.Security.Cryptography;

using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopVideoHashService : IDesktopVideoHashService
{
    private const int BufferSize = 1024 * 1024;

    public async ValueTask<DesktopVideoHashResult> CalculateSha256Async(
        DesktopVideoHashRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.TryGetLocalFilePath(out string filePath))
        {
            return DesktopVideoHashResult.Unavailable;
        }

        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return DesktopVideoHashResult.Succeeded(Convert.ToHexString(hash).ToLowerInvariant());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return DesktopVideoHashResult.Canceled;
        }
        catch (Exception exception) when (IsSafeHashFailure(exception))
        {
            return DesktopVideoHashResult.Unavailable;
        }
    }

    private static bool IsSafeHashFailure(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException;
    }
}