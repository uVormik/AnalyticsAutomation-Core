using System.Globalization;
using System.IO;

namespace App.Desktop.Services.Upload;

public sealed class DesktopDirectSiteUploadRequest
{
    private const int MaxBusinessObjectKeyLength = 128;
    private const int MaxContentTypeLength = 100;
    private const int MaxDisplayFileNameLength = 255;
    private const int MaxIdempotencyKeyLength = 128;
    private const string InvalidDisplay = "<invalid>";

    private static readonly string[] SensitiveFragments =
    [
        "authorization",
        "bearer",
        "credential",
        "password",
        "refresh",
        "secret",
        "token"
    ];

    private DesktopDirectSiteUploadRequest(
        bool isValid,
        Guid? preUploadCheckId,
        string? businessObjectKey,
        long sizeBytes,
        string? byteSha256,
        string? contentType,
        string? idempotencyKey,
        Guid? correlationId,
        string? displayFileName,
        DesktopDirectSiteUploadTarget uploadTarget)
    {
        IsValid = isValid;
        PreUploadCheckId = preUploadCheckId;
        BusinessObjectKey = businessObjectKey;
        SizeBytes = sizeBytes;
        ByteSha256 = byteSha256;
        ContentType = contentType;
        IdempotencyKey = idempotencyKey;
        CorrelationId = correlationId;
        DisplayFileName = displayFileName;
        UploadTarget = uploadTarget;
    }

    public bool IsValid { get; }

    public bool EnablesRealUpload { get; }

    public Guid? PreUploadCheckId { get; }

    public string? BusinessObjectKey { get; }

    public long SizeBytes { get; }

    public string? ByteSha256 { get; }

    public string? ContentType { get; }

    public string? IdempotencyKey { get; }

    public Guid? CorrelationId { get; }

    public string? DisplayFileName { get; }

    public string? ProviderKey => UploadTarget.ProviderKey;

    public DesktopDirectSiteUploadTarget UploadTarget { get; }

    public string RedactedUploadTarget => UploadTarget.RedactedDisplayUri;

    public string DiagnosticText => ToString();

    public static DesktopDirectSiteUploadRequest Invalid { get; } = new(
        isValid: false,
        preUploadCheckId: null,
        businessObjectKey: null,
        sizeBytes: 0,
        byteSha256: null,
        contentType: null,
        idempotencyKey: null,
        correlationId: null,
        displayFileName: null,
        DesktopDirectSiteUploadTarget.Unavailable);

    public static DesktopDirectSiteUploadRequest FromMetadata(
        DesktopDirectSiteUploadTarget? uploadTarget,
        string? preUploadCheckId,
        string? businessObjectKey,
        long sizeBytes,
        string? byteSha256,
        string? contentType,
        string? idempotencyKey,
        string? correlationId,
        string? displayFileName)
    {
        if (uploadTarget is null || !uploadTarget.IsValid)
        {
            return Invalid;
        }

        Guid? normalizedPreUploadCheckId = NormalizeGuid(preUploadCheckId);
        string? normalizedBusinessObjectKey = NormalizeBusinessObjectKey(businessObjectKey);
        string? normalizedSha256 = NormalizeSha256(byteSha256);
        string? normalizedContentType = NormalizeContentType(contentType);
        string? normalizedIdempotencyKey = NormalizeSafeToken(idempotencyKey, MaxIdempotencyKeyLength);
        Guid? normalizedCorrelationId = NormalizeGuid(correlationId);
        string? normalizedDisplayFileName = NormalizeDisplayFileName(displayFileName);

        if (normalizedPreUploadCheckId is null
            || normalizedBusinessObjectKey is null
            || sizeBytes <= 0
            || normalizedSha256 is null
            || normalizedContentType is null
            || normalizedIdempotencyKey is null
            || normalizedCorrelationId is null
            || normalizedDisplayFileName is null)
        {
            return Invalid;
        }

        return new DesktopDirectSiteUploadRequest(
            isValid: true,
            normalizedPreUploadCheckId,
            normalizedBusinessObjectKey,
            sizeBytes,
            normalizedSha256,
            normalizedContentType,
            normalizedIdempotencyKey,
            normalizedCorrelationId,
            normalizedDisplayFileName,
            uploadTarget);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopDirectSiteUploadRequest)} {{ "
            + $"IsValid = {IsValid}, "
            + $"EnablesRealUpload = {EnablesRealUpload}, "
            + $"ProviderKey = {ProviderKey ?? "<none>"}, "
            + $"UploadTarget = {RedactedUploadTarget}, "
            + $"HasPreUploadCheckId = {PreUploadCheckId.HasValue}, "
            + $"BusinessObjectKey = {BusinessObjectKey ?? InvalidDisplay}, "
            + $"SizeBytes = {SizeBytes.ToString(CultureInfo.InvariantCulture)}, "
            + $"HasSha256 = {ByteSha256?.Length == 64}, "
            + $"ContentType = {ContentType ?? InvalidDisplay}, "
            + $"HasIdempotencyKey = {!string.IsNullOrWhiteSpace(IdempotencyKey)}, "
            + $"HasCorrelationId = {CorrelationId.HasValue}, "
            + $"DisplayFileName = {DisplayFileName ?? InvalidDisplay} }}";
    }

    private static Guid? NormalizeGuid(string? value)
    {
        return Guid.TryParse(value?.Trim(), out Guid parsed)
            ? parsed
            : null;
    }

    private static string? NormalizeBusinessObjectKey(string? value)
    {
        string? normalized = NormalizeSafeSingleLine(value, MaxBusinessObjectKeyLength, allowSlash: false);
        return normalized is null || ContainsSensitiveFragment(normalized)
            ? null
            : normalized;
    }

    private static string? NormalizeContentType(string? value)
    {
        string? normalized = NormalizeSafeSingleLine(value, MaxContentTypeLength, allowSlash: true);
        if (normalized is null
            || ContainsSensitiveFragment(normalized)
            || normalized.StartsWith('/')
            || normalized.EndsWith('/')
            || !normalized.Contains('/', StringComparison.Ordinal))
        {
            return null;
        }

        return normalized;
    }

    private static string? NormalizeDisplayFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string fileName = Path.GetFileName(value.Trim());
        string? normalized = NormalizeSafeSingleLine(fileName, MaxDisplayFileNameLength, allowSlash: false);
        return normalized is null || ContainsSensitiveFragment(normalized)
            ? null
            : normalized;
    }

    private static string? NormalizeSha256(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return null;
        }

        foreach (char character in value)
        {
            bool isDigit = character is >= '0' and <= '9';
            bool isLowerHex = character is >= 'a' and <= 'f';
            if (!isDigit && !isLowerHex)
            {
                return null;
            }
        }

        return value;
    }

    private static string? NormalizeSafeToken(string? value, int maxLength)
    {
        string? normalized = NormalizeSafeSingleLine(value, maxLength, allowSlash: false);
        if (normalized is null || ContainsSensitiveFragment(normalized))
        {
            return null;
        }

        foreach (char character in normalized)
        {
            if (!char.IsLetterOrDigit(character)
                && character is not '-' and not '_' and not '.' and not ':')
            {
                return null;
            }
        }

        return normalized;
    }

    private static string? NormalizeSafeSingleLine(string? value, int maxLength, bool allowSlash)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            return null;
        }

        foreach (char character in trimmed)
        {
            if (char.IsControl(character)
                || character is '\\'
                || (!allowSlash && character is '/'))
            {
                return null;
            }
        }

        return trimmed;
    }

    private static bool ContainsSensitiveFragment(string value)
    {
        foreach (string sensitiveFragment in SensitiveFragments)
        {
            if (value.Contains(sensitiveFragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}