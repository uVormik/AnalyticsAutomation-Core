namespace BuildingBlocks.Contracts.VideoUpload;

public static class VideoUploadReceiptStatuses
{
    public const string Accepted = "ACCEPTED";
    public const string AlreadyAccepted = "ALREADY_ACCEPTED";
    public const string Rejected = "REJECTED";
}

public sealed record VideoUploadReceiptRequestDto(
    Guid PreUploadCheckId,
    Guid UserId,
    Guid? DeviceId,
    Guid? GroupNodeId,
    string ExternalVideoId,
    string StorageKey,
    string SiteStatus,
    long SizeBytes,
    string ByteSha256,
    string IdempotencyKey,
    DateTimeOffset UploadedAtUtc);

public sealed record VideoUploadReceiptResponseDto(
    Guid UploadReceiptId,
    Guid PreUploadCheckId,
    string Status,
    bool Accepted,
    bool WasAlreadyAccepted,
    string Message,
    string AnalysisJobStatus,
    DateTimeOffset ReceivedAtUtc);