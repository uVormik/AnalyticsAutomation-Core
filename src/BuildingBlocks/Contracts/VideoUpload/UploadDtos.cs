using System;

namespace BuildingBlocks.Contracts.VideoUpload;

public enum UploadPrecheckDecision
{
    Allow = 0,
    BlockHardDuplicate = 1,
    AllowWithReview = 2,
    BlockPossibleFalsification = 3
}

public enum DuplicateMatchKind
{
    None = 0,
    HardDuplicate = 1,
    SuspectDuplicate = 2,
    PossibleFalsification = 3
}

public sealed record UploadPrecheckRequestDto(
    Guid BusinessObjectId,
    string BusinessObjectType,
    Guid? DeviceId,
    string FileName,
    long SizeBytes,
    string ByteSha256,
    string? MimeType,
    DateTimeOffset ClientUtcNow);

public sealed record UploadPrecheckResponseDto(
    UploadPrecheckDecision Decision,
    DuplicateMatchKind MatchKind,
    string? ReasonCode,
    Guid? ExistingVideoAssetId,
    Guid? IncidentId);

public sealed record UploadReceiptDto(
    Guid ClientOperationId,
    Guid BusinessObjectId,
    string BusinessObjectType,
    string SiteVideoId,
    Guid? DeviceId,
    string FileName,
    long SizeBytes,
    string ByteSha256,
    DateTimeOffset UploadedAtUtc);

public sealed record UploadReceiptAcceptedDto(
    Guid ReceiptId,
    Guid VideoAssetId,
    bool RequiresReview);