using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopSiteUploadRequestPreview
{
    private DesktopSiteUploadRequestPreview(
        string fileName,
        long sizeBytes,
        string contentType,
        string sha256Hex,
        string businessObjectKeyPreview,
        string preUploadCheckDecisionPreview,
        string capturedAtUtc,
        Guid? preUploadCheckId,
        Guid? groupNodeId,
        string? plannedExternalVideoId,
        string? plannedSiteStorageKey)
    {
        FileName = fileName;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        Sha256Hex = sha256Hex;
        BusinessObjectKeyPreview = businessObjectKeyPreview;
        PreUploadCheckDecisionPreview = preUploadCheckDecisionPreview;
        CapturedAtUtc = capturedAtUtc;
        PreUploadCheckId = preUploadCheckId;
        GroupNodeId = groupNodeId;
        PlannedExternalVideoId = plannedExternalVideoId;
        PlannedSiteStorageKey = plannedSiteStorageKey;
    }

    public string FileName { get; }

    public long SizeBytes { get; }

    public string ContentType { get; }

    public string Sha256Hex { get; }

    public string BusinessObjectKeyPreview { get; }

    public string PreUploadCheckDecisionPreview { get; }

    public string CapturedAtUtc { get; }

    public Guid? PreUploadCheckId { get; }

    public Guid? GroupNodeId { get; }

    public string? PlannedExternalVideoId { get; }

    public string? PlannedSiteStorageKey { get; }

    public static DesktopSiteUploadRequestPreview? TryCreate(
        DesktopPreUploadCheckRequestPreview? preUploadCheckRequestPreview,
        DesktopPreUploadCheckResult? preUploadCheckResult)
    {
        if (preUploadCheckRequestPreview is null || preUploadCheckResult?.Decision is null)
        {
            return null;
        }

        DesktopPreUploadCheckDecision decision = preUploadCheckResult.Decision.Value;
        if (!IsAllowedSiteUploadDecision(decision))
        {
            return null;
        }

        string? decisionPreview = preUploadCheckResult.DecisionPreview;
        if (string.IsNullOrWhiteSpace(decisionPreview))
        {
            return null;
        }

        return new DesktopSiteUploadRequestPreview(
            preUploadCheckRequestPreview.FileName,
            preUploadCheckRequestPreview.SizeBytes,
            preUploadCheckRequestPreview.ContentType,
            preUploadCheckRequestPreview.Sha256Hex,
            preUploadCheckRequestPreview.BusinessObjectKeyPreview,
            decisionPreview,
            preUploadCheckRequestPreview.CapturedAtUtc,
            preUploadCheckResult.PreUploadCheckId,
            preUploadCheckRequestPreview.GroupNodeId,
            preUploadCheckResult.SitePlanExternalVideoId,
            preUploadCheckResult.SitePlanStorageKey);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopSiteUploadRequestPreview)} {{ FileName = {FileName}, "
            + $"SizeBytes = {SizeBytes}, ContentType = {ContentType}, HasSha256 = {Sha256Hex.Length == 64}, "
            + $"BusinessObjectKeyPreview = {BusinessObjectKeyPreview}, "
            + $"PreUploadCheckDecisionPreview = {PreUploadCheckDecisionPreview}, "
            + $"HasPreUploadCheckId = {PreUploadCheckId.HasValue}, HasGroupNodeId = {GroupNodeId.HasValue}, "
            + $"HasPlannedSiteTarget = {!string.IsNullOrWhiteSpace(PlannedExternalVideoId) && !string.IsNullOrWhiteSpace(PlannedSiteStorageKey)}, "
            + $"CapturedAtUtc = {CapturedAtUtc} }}";
    }

    private static bool IsAllowedSiteUploadDecision(DesktopPreUploadCheckDecision decision)
    {
        return decision is DesktopPreUploadCheckDecision.Allow
            or DesktopPreUploadCheckDecision.AllowWithReview;
    }
}

public sealed class DesktopSiteUploadResult
{
    private DesktopSiteUploadResult(
        DesktopSiteUploadStatus status,
        string message,
        string? externalVideoId,
        string? siteStorageKey)
    {
        Status = status;
        Message = message;
        ExternalVideoId = externalVideoId;
        SiteStorageKey = siteStorageKey;
    }

    public DesktopSiteUploadStatus Status { get; }

    public string? StatusPreview => Status switch
    {
        DesktopSiteUploadStatus.Succeeded => "SUCCESS",
        _ => null
    };

    public string Message { get; }

    public string? ExternalVideoId { get; }

    public string? SiteStorageKey { get; }

    public static DesktopSiteUploadResult Deferred { get; } = new(
        DesktopSiteUploadStatus.Deferred,
        DesktopUploadSectionText.SiteUploadDeferredMessage,
        externalVideoId: null,
        siteStorageKey: null);

    public static DesktopSiteUploadResult Canceled { get; } = new(
        DesktopSiteUploadStatus.Canceled,
        DesktopUploadSectionText.SiteUploadCanceledMessage,
        externalVideoId: null,
        siteStorageKey: null);

    public static DesktopSiteUploadResult FakeSuccess { get; } = new(
        DesktopSiteUploadStatus.Succeeded,
        DesktopUploadSectionText.SiteUploadSuccessDevMessage,
        externalVideoId: "visual-smoke-external-video-001",
        siteStorageKey: "visual-smoke/site/video-001");

    public static DesktopSiteUploadResult FromFakeSuccess(DesktopSiteUploadRequestPreview requestPreview)
    {
        ArgumentNullException.ThrowIfNull(requestPreview);

        return new DesktopSiteUploadResult(
            DesktopSiteUploadStatus.Succeeded,
            DesktopUploadSectionText.SiteUploadSuccessDevMessage,
            string.IsNullOrWhiteSpace(requestPreview.PlannedExternalVideoId)
                ? FakeSuccess.ExternalVideoId
                : requestPreview.PlannedExternalVideoId,
            string.IsNullOrWhiteSpace(requestPreview.PlannedSiteStorageKey)
                ? FakeSuccess.SiteStorageKey
                : requestPreview.PlannedSiteStorageKey);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopSiteUploadResult)} {{ Status = {StatusPreview ?? Status.ToString()}, "
            + $"HasExternalVideoId = {ExternalVideoId is not null}, HasSiteStorageKey = {SiteStorageKey is not null}, "
            + $"Message = {Message} }}";
    }
}

public enum DesktopSiteUploadStatus
{
    Deferred,
    Succeeded,
    Canceled
}