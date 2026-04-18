namespace BuildingBlocks.Contracts.VideoUpload;

public static class PreUploadCheckDecisions
{
    public const string Allow = "ALLOW";
    public const string BlockHardDuplicate = "BLOCK_HARD_DUPLICATE";
    public const string AllowWithReview = "ALLOW_WITH_REVIEW";
    public const string BlockPossibleFalsification = "BLOCK_POSSIBLE_FALSIFICATION";
}

public sealed record VideoPreUploadCheckRequestDto(
    Guid UserId,
    Guid? DeviceId,
    Guid? GroupNodeId,
    string BusinessObjectKey,
    string FileName,
    long SizeBytes,
    string ByteSha256,
    string? ContentType,
    DateTimeOffset CapturedAtUtc);

public sealed record VideoPreUploadSitePlanDto(
    string Provider,
    string ExternalVideoId,
    string StorageKey,
    string RequiredReceiptEndpoint);

public sealed record VideoPreUploadCheckResponseDto(
    Guid PreUploadCheckId,
    string Decision,
    bool CanUploadToSite,
    string ReasonCode,
    string Message,
    string? ExistingPreUploadCheckId,
    IReadOnlyCollection<string> RequiredNextSteps,
    VideoPreUploadSitePlanDto? SitePlan,
    DateTimeOffset CheckedAtUtc);