using System.Globalization;
using System.Security.Cryptography;

namespace App.Web.Features.Upload.Models;

public sealed record UploadSelectedVideoFileMetadata(
    string FileName,
    long SizeBytes,
    string ContentType,
    string ByteSha256,
    DateTimeOffset CapturedAtUtc);

public static class UploadSelectedVideoFileMetadataFactory
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".m4v",
        ".mov",
        ".webm",
        ".avi",
        ".mkv"
    };

    public static UploadSelectedVideoFileMetadata Create(
        string fileName,
        long sizeBytes,
        string? contentType,
        string byteSha256,
        DateTimeOffset capturedAtUtc)
    {
        string safeFileName = Required(fileName, nameof(fileName));
        string safeContentType = ResolveContentType(safeFileName, contentType);
        string safeSha256 = Required(byteSha256, nameof(byteSha256)).ToLowerInvariant();

        if (sizeBytes <= 0)
        {
            throw new InvalidOperationException("Selected video file must be greater than zero bytes.");
        }

        if (!IsSha256Hex(safeSha256))
        {
            throw new InvalidOperationException("Selected video file hash must be a SHA-256 hex value.");
        }

        if (!LooksLikeVideo(safeFileName, safeContentType))
        {
            throw new InvalidOperationException("Selected file must be a video file.");
        }

        return new UploadSelectedVideoFileMetadata(
            safeFileName,
            sizeBytes,
            safeContentType,
            safeSha256,
            capturedAtUtc.ToUniversalTime());
    }

    public static async Task<string> ComputeSha256HexAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("Selected video file stream must be readable.");
        }

        using SHA256 sha256 = SHA256.Create();
        byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Required(string value, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"{fieldName} is required.");
    }

    private static string ResolveContentType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType.Trim();
        }

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".m4v" => "video/mp4",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream"
        };
    }

    private static bool LooksLikeVideo(string fileName, string contentType)
    {
        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return VideoExtensions.Contains(Path.GetExtension(fileName));
    }

    private static bool IsSha256Hex(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        return value.All(static character =>
            (character >= '0' && character <= '9')
            || (character >= 'a' && character <= 'f'));
    }
}

public static class UploadPreCheckFormAutoFill
{
    public static void ApplySelectedFile(
        UploadPreCheckFormModel form,
        UploadSelectedVideoFileMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(metadata);

        form.FileName = metadata.FileName;
        form.SizeBytes = metadata.SizeBytes;
        form.ContentType = metadata.ContentType;
        form.ByteSha256 = metadata.ByteSha256;
        form.CapturedAtUtc = metadata.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture);
    }
}