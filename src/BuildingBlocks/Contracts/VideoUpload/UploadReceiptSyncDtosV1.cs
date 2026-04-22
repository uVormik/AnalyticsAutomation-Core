namespace BuildingBlocks.Contracts.VideoUpload;

public sealed record VideoUploadReceiptSyncRequestDto(
    Guid UserId,
    Guid? DeviceId,
    Guid? GroupNodeId,
    string BusinessObjectKey,
    string FileName,
    string? ContentType,
    string ExternalVideoId,
    string StorageKey,
    string SiteStatus,
    long SizeBytes,
    string ByteSha256,
    string IdempotencyKey,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset UploadedAtUtc);