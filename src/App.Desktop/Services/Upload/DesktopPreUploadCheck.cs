using App.Desktop.Boundaries;
using App.Desktop.Services.GroupTree;

namespace App.Desktop.Services.Upload;

public sealed class DesktopPreUploadCheckRequestPreview
{
    public const string CapturedAtUtcPlaceholder = "desktop-local-preview";

    private DesktopPreUploadCheckRequestPreview(
        string fileName,
        long sizeBytes,
        string contentType,
        string sha256Hex,
        string businessObjectKeyPreview,
        string capturedAtUtc,
        Guid? groupNodeId)
    {
        FileName = fileName;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        Sha256Hex = sha256Hex;
        BusinessObjectKeyPreview = businessObjectKeyPreview;
        CapturedAtUtc = capturedAtUtc;
        GroupNodeId = groupNodeId;
    }

    public string FileName { get; }

    public long SizeBytes { get; }

    public string ContentType { get; }

    public string Sha256Hex { get; }

    public string BusinessObjectKeyPreview { get; }

    public string CapturedAtUtc { get; }

    public Guid? GroupNodeId { get; }

    public static DesktopPreUploadCheckRequestPreview? TryCreate(
        DesktopUploadSelectedFile? selectedFile,
        string? sha256Hex,
        DesktopUploadBusinessObjectKey? businessObjectKey,
        DesktopSelectedGroupContext? selectedGroupContext = null)
    {
        string safeSha256Hex = sha256Hex ?? string.Empty;

        if (selectedFile is null
            || !IsLowercaseSha256Hex(safeSha256Hex)
            || businessObjectKey is null
            || !IsSafeBusinessObjectKeyPreview(businessObjectKey.Value))
        {
            return null;
        }

        return new DesktopPreUploadCheckRequestPreview(
            selectedFile.FileName,
            selectedFile.SizeBytes,
            selectedFile.ContentType,
            safeSha256Hex,
            businessObjectKey.Value,
            CapturedAtUtcPlaceholder,
            TryParseGroupNodeId(selectedGroupContext));
    }

    public override string ToString()
    {
        return $"{nameof(DesktopPreUploadCheckRequestPreview)} {{ FileName = {FileName}, "
            + $"SizeBytes = {SizeBytes}, ContentType = {ContentType}, HasSha256 = {Sha256Hex.Length == 64}, "
            + $"BusinessObjectKeyPreview = {BusinessObjectKeyPreview}, CapturedAtUtc = {CapturedAtUtc}, "
            + $"HasGroupNodeId = {GroupNodeId.HasValue} }}";
    }

    private static bool IsLowercaseSha256Hex(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isDigit = character is >= '0' and <= '9';
            bool isLowerHex = character is >= 'a' and <= 'f';
            if (!isDigit && !isLowerHex)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeBusinessObjectKeyPreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > DesktopUploadBusinessObjectKey.MaxLength)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (char.IsControl(character))
            {
                return false;
            }
        }

        return true;
    }

    private static Guid? TryParseGroupNodeId(DesktopSelectedGroupContext? selectedGroupContext)
    {
        return Guid.TryParse(selectedGroupContext?.Id, out Guid groupNodeId)
            ? groupNodeId
            : null;
    }
}

public sealed class DesktopPreUploadCheckResult
{
    private DesktopPreUploadCheckResult(
        DesktopPreUploadCheckStatus status,
        DesktopPreUploadCheckDecision? decision,
        string message)
    {
        Status = status;
        Decision = decision;
        Message = message;
    }

    public DesktopPreUploadCheckStatus Status { get; }

    public DesktopPreUploadCheckDecision? Decision { get; }

    public string? DecisionPreview => Decision switch
    {
        DesktopPreUploadCheckDecision.Allow => "ALLOW",
        DesktopPreUploadCheckDecision.AllowWithReview => "ALLOW_WITH_REVIEW",
        DesktopPreUploadCheckDecision.BlockHardDuplicate => "BLOCK_HARD_DUPLICATE",
        DesktopPreUploadCheckDecision.BlockPossibleFalsification => "BLOCK_POSSIBLE_FALSIFICATION",
        _ => null
    };

    public string Message { get; }

    public static DesktopPreUploadCheckResult Deferred { get; } = new(
        DesktopPreUploadCheckStatus.Deferred,
        decision: null,
        DesktopUploadSectionText.PreUploadCheckDeferredMessage);

    public static DesktopPreUploadCheckResult Canceled { get; } = new(
        DesktopPreUploadCheckStatus.Canceled,
        decision: null,
        DesktopUploadSectionText.PreUploadCheckCanceledMessage);

    public static DesktopPreUploadCheckResult FromFakeDecision(DesktopPreUploadCheckDecision decision)
    {
        string message = decision switch
        {
            DesktopPreUploadCheckDecision.Allow => DesktopUploadSectionText.PreUploadCheckAllowedDevMessage,
            DesktopPreUploadCheckDecision.AllowWithReview =>
                DesktopUploadSectionText.PreUploadCheckAllowWithReviewDevMessage,
            DesktopPreUploadCheckDecision.BlockHardDuplicate =>
                DesktopUploadSectionText.PreUploadCheckBlockHardDuplicateDevMessage,
            DesktopPreUploadCheckDecision.BlockPossibleFalsification =>
                DesktopUploadSectionText.PreUploadCheckBlockPossibleFalsificationDevMessage,
            _ => DesktopUploadSectionText.PreUploadCheckDeferredMessage
        };

        DesktopPreUploadCheckStatus status = decision switch
        {
            DesktopPreUploadCheckDecision.Allow => DesktopPreUploadCheckStatus.Allowed,
            DesktopPreUploadCheckDecision.AllowWithReview => DesktopPreUploadCheckStatus.AllowedWithReview,
            _ => DesktopPreUploadCheckStatus.Blocked
        };

        return new DesktopPreUploadCheckResult(status, decision, message);
    }

    public static DesktopPreUploadCheckResult FromLiveDecision(DesktopPreUploadCheckDecision decision)
    {
        string message = decision switch
        {
            DesktopPreUploadCheckDecision.Allow => DesktopUploadSectionText.PreUploadCheckAllowedLiveMessage,
            DesktopPreUploadCheckDecision.AllowWithReview =>
                DesktopUploadSectionText.PreUploadCheckAllowWithReviewLiveMessage,
            DesktopPreUploadCheckDecision.BlockHardDuplicate =>
                DesktopUploadSectionText.PreUploadCheckBlockHardDuplicateLiveMessage,
            DesktopPreUploadCheckDecision.BlockPossibleFalsification =>
                DesktopUploadSectionText.PreUploadCheckBlockPossibleFalsificationLiveMessage,
            _ => DesktopUploadSectionText.PreUploadCheckLiveMalformedMessage
        };

        DesktopPreUploadCheckStatus status = decision switch
        {
            DesktopPreUploadCheckDecision.Allow => DesktopPreUploadCheckStatus.Allowed,
            DesktopPreUploadCheckDecision.AllowWithReview => DesktopPreUploadCheckStatus.AllowedWithReview,
            _ => DesktopPreUploadCheckStatus.Blocked
        };

        return new DesktopPreUploadCheckResult(status, decision, message);
    }

    public static DesktopPreUploadCheckResult LiveUnavailable { get; } = new(
        DesktopPreUploadCheckStatus.Unavailable,
        decision: null,
        DesktopUploadSectionText.PreUploadCheckLiveUnavailableMessage);

    public static DesktopPreUploadCheckResult LiveUnauthorized { get; } = new(
        DesktopPreUploadCheckStatus.Unauthorized,
        decision: null,
        DesktopUploadSectionText.PreUploadCheckLiveUnauthorizedMessage);

    public static DesktopPreUploadCheckResult LiveFailed { get; } = new(
        DesktopPreUploadCheckStatus.Failed,
        decision: null,
        DesktopUploadSectionText.PreUploadCheckLiveFailedMessage);

    public static DesktopPreUploadCheckResult LiveMalformed { get; } = new(
        DesktopPreUploadCheckStatus.Malformed,
        decision: null,
        DesktopUploadSectionText.PreUploadCheckLiveMalformedMessage);

    public override string ToString()
    {
        return $"{nameof(DesktopPreUploadCheckResult)} {{ Status = {Status}, Decision = {DecisionPreview ?? "<none>"}, "
            + $"Message = {Message} }}";
    }
}

public enum DesktopPreUploadCheckStatus
{
    Deferred,
    Allowed,
    AllowedWithReview,
    Blocked,
    Canceled,
    Unavailable,
    Unauthorized,
    Failed,
    Malformed
}

public enum DesktopPreUploadCheckDecision
{
    Allow,
    AllowWithReview,
    BlockHardDuplicate,
    BlockPossibleFalsification
}