using System.Globalization;

namespace App.Desktop.Services.Upload;

public sealed class DesktopDirectSiteUploadResponse
{
    private const int MaxSafeIdentifierLength = 128;
    private const string RedactedFailureMessage = "<redacted>";

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

    private DesktopDirectSiteUploadResponse(
        bool isValid,
        DesktopDirectSiteUploadResponseStatus status,
        string? providerKey,
        string? providerUploadId,
        string? externalVideoId,
        string? storageKey,
        DateTimeOffset? uploadedAtUtc,
        long? sizeBytes,
        string? byteSha256,
        Guid? correlationId,
        bool retryable,
        DesktopDirectSiteUploadFailureKind failureKind,
        string? failureMessageRedacted)
    {
        IsValid = isValid;
        Status = status;
        ProviderKey = providerKey;
        ProviderUploadId = providerUploadId;
        ExternalVideoId = externalVideoId;
        StorageKey = storageKey;
        UploadedAtUtc = uploadedAtUtc;
        SizeBytes = sizeBytes;
        ByteSha256 = byteSha256;
        CorrelationId = correlationId;
        Retryable = retryable;
        FailureKind = failureKind;
        FailureMessageRedacted = failureMessageRedacted;
    }

    public bool IsValid { get; }

    public bool EnablesRealUpload { get; }

    public bool IsSuccess => IsValid
        && Status is DesktopDirectSiteUploadResponseStatus.Accepted
            or DesktopDirectSiteUploadResponseStatus.Completed
            or DesktopDirectSiteUploadResponseStatus.DuplicateSuspected;

    public DesktopDirectSiteUploadResponseStatus Status { get; }

    public string? ProviderKey { get; }

    public string? ProviderUploadId { get; }

    public string? ExternalVideoId { get; }

    public string? StorageKey { get; }

    public DateTimeOffset? UploadedAtUtc { get; }

    public long? SizeBytes { get; }

    public string? ByteSha256 { get; }

    public Guid? CorrelationId { get; }

    public bool Retryable { get; }

    public DesktopDirectSiteUploadFailureKind FailureKind { get; }

    public string? FailureMessageRedacted { get; }

    public string DiagnosticText => ToString();

    public static DesktopDirectSiteUploadResponse Invalid { get; } = new(
        isValid: false,
        DesktopDirectSiteUploadResponseStatus.Unknown,
        providerKey: null,
        providerUploadId: null,
        externalVideoId: null,
        storageKey: null,
        uploadedAtUtc: null,
        sizeBytes: null,
        byteSha256: null,
        correlationId: null,
        retryable: false,
        DesktopDirectSiteUploadFailureKind.Unknown,
        failureMessageRedacted: null);

    public static DesktopDirectSiteUploadResponse FromSuccess(
        string? providerKey,
        string? providerUploadId,
        string? externalVideoId,
        string? storageKey,
        DesktopDirectSiteUploadResponseStatus status,
        DateTimeOffset uploadedAtUtc,
        long sizeBytes,
        string? byteSha256,
        string? correlationId)
    {
        string? normalizedProviderKey = NormalizeSafeIdentifier(providerKey, allowSlash: false);
        string? normalizedProviderUploadId = NormalizeSafeIdentifier(providerUploadId, allowSlash: false);
        string? normalizedExternalVideoId = NormalizeSafeIdentifier(externalVideoId, allowSlash: false);
        string? normalizedStorageKey = NormalizeSafeIdentifier(storageKey, allowSlash: true);
        string? normalizedSha256 = NormalizeSha256(byteSha256);
        Guid? normalizedCorrelationId = NormalizeGuid(correlationId);

        if (normalizedProviderKey is null
            || normalizedProviderUploadId is null
            || normalizedExternalVideoId is null
            || normalizedStorageKey is null
            || normalizedSha256 is null
            || normalizedCorrelationId is null
            || sizeBytes <= 0
            || status is not (DesktopDirectSiteUploadResponseStatus.Accepted
                or DesktopDirectSiteUploadResponseStatus.Completed
                or DesktopDirectSiteUploadResponseStatus.DuplicateSuspected))
        {
            return Invalid;
        }

        return new DesktopDirectSiteUploadResponse(
            isValid: true,
            status,
            normalizedProviderKey,
            normalizedProviderUploadId,
            normalizedExternalVideoId,
            normalizedStorageKey,
            uploadedAtUtc,
            sizeBytes,
            normalizedSha256,
            normalizedCorrelationId,
            retryable: false,
            DesktopDirectSiteUploadFailureKind.None,
            failureMessageRedacted: null);
    }

    public static DesktopDirectSiteUploadResponse FromFailure(
        string? providerKey,
        DesktopDirectSiteUploadResponseStatus status,
        bool retryable,
        DesktopDirectSiteUploadFailureKind failureKind,
        string? failureMessage,
        string? correlationId)
    {
        string? normalizedProviderKey = NormalizeSafeIdentifier(providerKey, allowSlash: false);
        Guid? normalizedCorrelationId = NormalizeGuid(correlationId);

        if (normalizedProviderKey is null
            || normalizedCorrelationId is null
            || failureKind is DesktopDirectSiteUploadFailureKind.None
            || status is not (DesktopDirectSiteUploadResponseStatus.Rejected
                or DesktopDirectSiteUploadResponseStatus.FailedRetryable
                or DesktopDirectSiteUploadResponseStatus.FailedTerminal
                or DesktopDirectSiteUploadResponseStatus.Unknown))
        {
            return Invalid;
        }

        return new DesktopDirectSiteUploadResponse(
            isValid: true,
            status,
            normalizedProviderKey,
            providerUploadId: null,
            externalVideoId: null,
            storageKey: null,
            uploadedAtUtc: null,
            sizeBytes: null,
            byteSha256: null,
            normalizedCorrelationId,
            retryable,
            failureKind,
            RedactFailureMessage(failureMessage));
    }

    public override string ToString()
    {
        return $"{nameof(DesktopDirectSiteUploadResponse)} {{ "
            + $"IsValid = {IsValid}, "
            + $"EnablesRealUpload = {EnablesRealUpload}, "
            + $"Status = {Status}, "
            + $"ProviderKey = {ProviderKey ?? "<none>"}, "
            + $"HasProviderUploadId = {!string.IsNullOrWhiteSpace(ProviderUploadId)}, "
            + $"HasExternalVideoId = {!string.IsNullOrWhiteSpace(ExternalVideoId)}, "
            + $"HasStorageKey = {!string.IsNullOrWhiteSpace(StorageKey)}, "
            + $"UploadedAtUtc = {FormatUploadedAtUtc()}, "
            + $"SizeBytes = {FormatSizeBytes()}, "
            + $"HasSha256 = {ByteSha256?.Length == 64}, "
            + $"HasCorrelationId = {CorrelationId.HasValue}, "
            + $"Retryable = {Retryable}, "
            + $"FailureKind = {FailureKind}, "
            + $"FailureMessage = {FailureMessageRedacted ?? "<none>"} }}";
    }

    private static string? RedactFailureMessage(string? failureMessage)
    {
        return string.IsNullOrWhiteSpace(failureMessage)
            ? null
            : RedactedFailureMessage;
    }

    private string FormatUploadedAtUtc()
    {
        return UploadedAtUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "<none>";
    }

    private string FormatSizeBytes()
    {
        return SizeBytes?.ToString(CultureInfo.InvariantCulture) ?? "<none>";
    }

    private static Guid? NormalizeGuid(string? value)
    {
        return Guid.TryParse(value?.Trim(), out Guid parsed)
            ? parsed
            : null;
    }

    private static string? NormalizeSafeIdentifier(string? value, bool allowSlash)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > MaxSafeIdentifierLength
            || ContainsSensitiveFragment(trimmed)
            || LooksLikeUri(trimmed)
            || LooksLikeLocalPath(trimmed))
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

    private static bool LooksLikeUri(string value)
    {
        return value.Contains("://", StringComparison.Ordinal)
            || value.Contains('?', StringComparison.Ordinal)
            || value.Contains('#', StringComparison.Ordinal);
    }

    private static bool LooksLikeLocalPath(string value)
    {
        return value.Length > 2 && char.IsLetter(value[0]) && value[1] == ':';
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

public enum DesktopDirectSiteUploadResponseStatus
{
    Accepted,
    Completed,
    Rejected,
    DuplicateSuspected,
    FailedRetryable,
    FailedTerminal,
    Unknown
}

public enum DesktopDirectSiteUploadFailureKind
{
    None,
    NetworkRetryable,
    ProviderTemporary,
    Validation,
    Unauthorized,
    ProviderUnavailable,
    ChecksumMismatch,
    SizeMismatch,
    UnsupportedMediaType,
    Unknown
}