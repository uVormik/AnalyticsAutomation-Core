using System.IO;

using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadReceiptRequestPreview
{
    private DesktopUploadReceiptRequestPreview(
        string fileName,
        long sizeBytes,
        string contentType,
        string sha256Hex,
        string businessObjectKeyPreview,
        string preUploadCheckDecisionPreview,
        string externalVideoId,
        string siteStorageKey,
        string siteUploadStatusPreview,
        string capturedAtUtc,
        Guid? preUploadCheckId,
        Guid? groupNodeId)
    {
        FileName = fileName;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        Sha256Hex = sha256Hex;
        BusinessObjectKeyPreview = businessObjectKeyPreview;
        PreUploadCheckDecisionPreview = preUploadCheckDecisionPreview;
        ExternalVideoId = externalVideoId;
        SiteStorageKey = siteStorageKey;
        SiteUploadStatusPreview = siteUploadStatusPreview;
        CapturedAtUtc = capturedAtUtc;
        PreUploadCheckId = preUploadCheckId;
        GroupNodeId = groupNodeId;
    }

    public string FileName { get; }

    public long SizeBytes { get; }

    public string ContentType { get; }

    public string Sha256Hex { get; }

    public string BusinessObjectKeyPreview { get; }

    public string PreUploadCheckDecisionPreview { get; }

    public string ExternalVideoId { get; }

    public string SiteStorageKey { get; }

    public string SiteUploadStatusPreview { get; }

    public string CapturedAtUtc { get; }

    public Guid? PreUploadCheckId { get; }

    public Guid? GroupNodeId { get; }

    public static DesktopUploadReceiptRequestPreview? TryCreate(
        DesktopSiteUploadRequestPreview? siteUploadRequestPreview,
        DesktopSiteUploadResult? siteUploadResult)
    {
        if (siteUploadRequestPreview is null
            || siteUploadResult?.Status != DesktopSiteUploadStatus.Succeeded
            || !TryCreateSafeReceiptValue(siteUploadResult.ExternalVideoId, allowSlash: false, out string externalVideoId)
            || !TryCreateSafeReceiptValue(siteUploadResult.SiteStorageKey, allowSlash: true, out string siteStorageKey)
            || !TryCreateSafeReceiptValue(siteUploadResult.StatusPreview, allowSlash: false, out string siteUploadStatusPreview))
        {
            return null;
        }

        return new DesktopUploadReceiptRequestPreview(
            siteUploadRequestPreview.FileName,
            siteUploadRequestPreview.SizeBytes,
            siteUploadRequestPreview.ContentType,
            siteUploadRequestPreview.Sha256Hex,
            siteUploadRequestPreview.BusinessObjectKeyPreview,
            siteUploadRequestPreview.PreUploadCheckDecisionPreview,
            externalVideoId,
            siteStorageKey,
            siteUploadStatusPreview,
            siteUploadRequestPreview.CapturedAtUtc,
            siteUploadRequestPreview.PreUploadCheckId,
            siteUploadRequestPreview.GroupNodeId);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopUploadReceiptRequestPreview)} {{ FileName = {FileName}, "
            + $"SizeBytes = {SizeBytes}, ContentType = {ContentType}, HasSha256 = {Sha256Hex.Length == 64}, "
            + $"BusinessObjectKeyPreview = {BusinessObjectKeyPreview}, "
            + $"PreUploadCheckDecisionPreview = {PreUploadCheckDecisionPreview}, "
            + $"ExternalVideoId = {ExternalVideoId}, SiteStorageKey = {SiteStorageKey}, "
            + $"SiteUploadStatusPreview = {SiteUploadStatusPreview}, "
            + $"HasPreUploadCheckId = {PreUploadCheckId.HasValue}, HasGroupNodeId = {GroupNodeId.HasValue}, "
            + $"CapturedAtUtc = {CapturedAtUtc} }}";
    }

    private static bool TryCreateSafeReceiptValue(string? value, bool allowSlash, out string safeValue)
    {
        safeValue = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 200)
        {
            return false;
        }

        string lower = trimmed.ToLowerInvariant();
        string[] blockedFragments =
        [
            "authorization",
            "bearer",
            "password",
            "token",
            "sessionid",
            "session_id",
            "access_token",
            "refreshtoken",
            "refresh_token"
        ];

        foreach (string blockedFragment in blockedFragments)
        {
            if (lower.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        foreach (char character in trimmed)
        {
            if (char.IsControl(character)
                || character == Path.DirectorySeparatorChar
                || character == Path.VolumeSeparatorChar
                || (!allowSlash && character == Path.AltDirectorySeparatorChar))
            {
                return false;
            }
        }

        safeValue = trimmed;
        return true;
    }
}

public sealed class DesktopUploadReceiptResult
{
    private DesktopUploadReceiptResult(
        DesktopUploadReceiptStatus status,
        string message,
        string? receiptId,
        string? serverCorrelationId)
    {
        Status = status;
        Message = message;
        ReceiptId = receiptId;
        ServerCorrelationId = serverCorrelationId;
    }

    public DesktopUploadReceiptStatus Status { get; }

    public string? StatusPreview => Status switch
    {
        DesktopUploadReceiptStatus.Accepted => "ACCEPTED",
        DesktopUploadReceiptStatus.AlreadyAccepted => "ALREADY_ACCEPTED",
        _ => null
    };

    public string Message { get; }

    public string? ReceiptId { get; }

    public string? ServerCorrelationId { get; }

    public static DesktopUploadReceiptResult Deferred { get; } = new(
        DesktopUploadReceiptStatus.Deferred,
        DesktopUploadSectionText.UploadReceiptDeferredMessage,
        receiptId: null,
        serverCorrelationId: null);

    public static DesktopUploadReceiptResult Canceled { get; } = new(
        DesktopUploadReceiptStatus.Canceled,
        DesktopUploadSectionText.UploadReceiptCanceledMessage,
        receiptId: null,
        serverCorrelationId: null);

    public static DesktopUploadReceiptResult FakeAccepted { get; } = new(
        DesktopUploadReceiptStatus.Accepted,
        DesktopUploadSectionText.UploadReceiptAcceptedDevMessage,
        receiptId: "visual-smoke-upload-receipt-001",
        serverCorrelationId: "visual-smoke-correlation-001");

    public static DesktopUploadReceiptResult FromLiveAccepted(
        Guid uploadReceiptId,
        bool wasAlreadyAccepted)
    {
        return new DesktopUploadReceiptResult(
            wasAlreadyAccepted
                ? DesktopUploadReceiptStatus.AlreadyAccepted
                : DesktopUploadReceiptStatus.Accepted,
            wasAlreadyAccepted
                ? DesktopUploadSectionText.UploadReceiptAlreadyAcceptedLiveMessage
                : DesktopUploadSectionText.UploadReceiptAcceptedLiveMessage,
            uploadReceiptId.ToString("D"),
            serverCorrelationId: null);
    }

    public static DesktopUploadReceiptResult LiveUnavailable { get; } = new(
        DesktopUploadReceiptStatus.Unavailable,
        DesktopUploadSectionText.UploadReceiptLiveUnavailableMessage,
        receiptId: null,
        serverCorrelationId: null);

    public static DesktopUploadReceiptResult LiveUnauthorized { get; } = new(
        DesktopUploadReceiptStatus.Unauthorized,
        DesktopUploadSectionText.UploadReceiptLiveUnauthorizedMessage,
        receiptId: null,
        serverCorrelationId: null);

    public static DesktopUploadReceiptResult LiveFailed { get; } = new(
        DesktopUploadReceiptStatus.Failed,
        DesktopUploadSectionText.UploadReceiptLiveFailedMessage,
        receiptId: null,
        serverCorrelationId: null);

    public static DesktopUploadReceiptResult LiveMalformed { get; } = new(
        DesktopUploadReceiptStatus.Malformed,
        DesktopUploadSectionText.UploadReceiptLiveMalformedMessage,
        receiptId: null,
        serverCorrelationId: null);

    public override string ToString()
    {
        return $"{nameof(DesktopUploadReceiptResult)} {{ Status = {StatusPreview ?? Status.ToString()}, "
            + $"HasReceiptId = {ReceiptId is not null}, HasServerCorrelationId = {ServerCorrelationId is not null}, "
            + $"Message = {Message} }}";
    }
}

public enum DesktopUploadReceiptStatus
{
    Deferred,
    Accepted,
    AlreadyAccepted,
    Canceled,
    Unavailable,
    Unauthorized,
    Failed,
    Malformed
}