using System.Globalization;

namespace App.Desktop.Services.Upload;

public sealed class DesktopDirectSiteUploadReceiptLinkage
{
    private DesktopDirectSiteUploadReceiptLinkage(
        bool isReadyForFutureServerSubmission,
        DesktopDirectSiteUploadReceiptLinkageStatus status,
        DesktopDirectSiteUploadReceiptLinkageFailureKind failureKind,
        Guid? preUploadCheckId,
        string? businessObjectKey,
        string? providerKey,
        string? providerUploadId,
        string? externalVideoId,
        string? storageKey,
        DateTimeOffset? uploadedAtUtc,
        long? sizeBytes,
        string? byteSha256,
        string? idempotencyKey,
        Guid? correlationId)
    {
        IsReadyForFutureServerSubmission = isReadyForFutureServerSubmission;
        Status = status;
        FailureKind = failureKind;
        PreUploadCheckId = preUploadCheckId;
        BusinessObjectKey = businessObjectKey;
        ProviderKey = providerKey;
        ProviderUploadId = providerUploadId;
        ExternalVideoId = externalVideoId;
        StorageKey = storageKey;
        UploadedAtUtc = uploadedAtUtc;
        SizeBytes = sizeBytes;
        ByteSha256 = byteSha256;
        IdempotencyKey = idempotencyKey;
        CorrelationId = correlationId;
    }

    public bool IsReadyForFutureServerSubmission { get; }

    public bool EnablesRealUpload { get; }

    public DesktopDirectSiteUploadReceiptLinkageStatus Status { get; }

    public DesktopDirectSiteUploadReceiptLinkageFailureKind FailureKind { get; }

    public Guid? PreUploadCheckId { get; }

    public string? BusinessObjectKey { get; }

    public string? ProviderKey { get; }

    public string? ProviderUploadId { get; }

    public string? ExternalVideoId { get; }

    public string? StorageKey { get; }

    public DateTimeOffset? UploadedAtUtc { get; }

    public long? SizeBytes { get; }

    public string? ByteSha256 { get; }

    public string? IdempotencyKey { get; }

    public Guid? CorrelationId { get; }

    public string DiagnosticText => ToString();

    public static DesktopDirectSiteUploadReceiptLinkage FromRequestAndResponse(
        DesktopDirectSiteUploadRequest? request,
        DesktopDirectSiteUploadResponse? response)
    {
        if (request is null || !request.IsValid)
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.InvalidRequest);
        }

        if (response is null || !response.IsValid)
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.InvalidResponse);
        }

        if (!response.IsSuccess)
        {
            return NotReady(MapFailureKind(response));
        }

        if (request.PreUploadCheckId is null
            || request.BusinessObjectKey is null
            || request.ProviderKey is null
            || request.ByteSha256 is null
            || request.IdempotencyKey is null
            || request.CorrelationId is null
            || response.ProviderKey is null
            || response.ProviderUploadId is null
            || response.ExternalVideoId is null
            || response.StorageKey is null
            || response.UploadedAtUtc is null
            || response.SizeBytes is null
            || response.ByteSha256 is null
            || response.CorrelationId is null)
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.MissingReceiptFields);
        }

        if (!string.Equals(request.ProviderKey, response.ProviderKey, StringComparison.Ordinal))
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderMismatch);
        }

        if (request.SizeBytes != response.SizeBytes.Value)
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.SizeMismatch);
        }

        if (!string.Equals(request.ByteSha256, response.ByteSha256, StringComparison.Ordinal))
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.Sha256Mismatch);
        }

        if (request.CorrelationId.Value != response.CorrelationId.Value)
        {
            return NotReady(DesktopDirectSiteUploadReceiptLinkageFailureKind.CorrelationMismatch);
        }

        return new DesktopDirectSiteUploadReceiptLinkage(
            isReadyForFutureServerSubmission: true,
            DesktopDirectSiteUploadReceiptLinkageStatus.ReadyForFutureServerSubmission,
            DesktopDirectSiteUploadReceiptLinkageFailureKind.None,
            request.PreUploadCheckId,
            request.BusinessObjectKey,
            request.ProviderKey,
            response.ProviderUploadId,
            response.ExternalVideoId,
            response.StorageKey,
            response.UploadedAtUtc.Value.ToUniversalTime(),
            response.SizeBytes,
            response.ByteSha256,
            request.IdempotencyKey,
            request.CorrelationId);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopDirectSiteUploadReceiptLinkage)} {{ "
            + $"IsReadyForFutureServerSubmission = {IsReadyForFutureServerSubmission}, "
            + $"EnablesRealUpload = {EnablesRealUpload}, "
            + $"Status = {Status}, "
            + $"FailureKind = {FailureKind}, "
            + $"HasPreUploadCheckId = {PreUploadCheckId.HasValue}, "
            + $"HasBusinessObjectKey = {!string.IsNullOrWhiteSpace(BusinessObjectKey)}, "
            + $"HasProviderKey = {!string.IsNullOrWhiteSpace(ProviderKey)}, "
            + $"HasProviderUploadId = {!string.IsNullOrWhiteSpace(ProviderUploadId)}, "
            + $"HasExternalVideoId = {!string.IsNullOrWhiteSpace(ExternalVideoId)}, "
            + $"HasStorageKey = {!string.IsNullOrWhiteSpace(StorageKey)}, "
            + $"UploadedAtUtc = {FormatUploadedAtUtc()}, "
            + $"SizeBytes = {FormatSizeBytes()}, "
            + $"HasSha256 = {ByteSha256?.Length == 64}, "
            + $"HasIdempotencyKey = {!string.IsNullOrWhiteSpace(IdempotencyKey)}, "
            + $"HasCorrelationId = {CorrelationId.HasValue} }}";
    }

    private static DesktopDirectSiteUploadReceiptLinkage NotReady(
        DesktopDirectSiteUploadReceiptLinkageFailureKind failureKind)
    {
        return new DesktopDirectSiteUploadReceiptLinkage(
            isReadyForFutureServerSubmission: false,
            failureKind is DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderUnavailable
                ? DesktopDirectSiteUploadReceiptLinkageStatus.Unavailable
                : DesktopDirectSiteUploadReceiptLinkageStatus.NotReady,
            failureKind,
            preUploadCheckId: null,
            businessObjectKey: null,
            providerKey: null,
            providerUploadId: null,
            externalVideoId: null,
            storageKey: null,
            uploadedAtUtc: null,
            sizeBytes: null,
            byteSha256: null,
            idempotencyKey: null,
            correlationId: null);
    }

    private static DesktopDirectSiteUploadReceiptLinkageFailureKind MapFailureKind(
        DesktopDirectSiteUploadResponse response)
    {
        if (response.FailureKind is DesktopDirectSiteUploadFailureKind.ProviderUnavailable)
        {
            return DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderUnavailable;
        }

        return response.Retryable
            ? DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderRetryableFailure
            : DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderFailure;
    }

    private string FormatUploadedAtUtc()
    {
        return UploadedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "<none>";
    }

    private string FormatSizeBytes()
    {
        return SizeBytes?.ToString(CultureInfo.InvariantCulture) ?? "<none>";
    }
}

public enum DesktopDirectSiteUploadReceiptLinkageStatus
{
    ReadyForFutureServerSubmission,
    NotReady,
    Unavailable
}

public enum DesktopDirectSiteUploadReceiptLinkageFailureKind
{
    None,
    InvalidRequest,
    InvalidResponse,
    ProviderUnavailable,
    ProviderFailure,
    ProviderRetryableFailure,
    MissingReceiptFields,
    ProviderMismatch,
    SizeMismatch,
    Sha256Mismatch,
    CorrelationMismatch
}