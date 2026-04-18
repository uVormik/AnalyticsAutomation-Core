namespace BuildingBlocks.Contracts.VideoDuplicates;

public static class DuplicateMatchKinds
{
    public const string HardDuplicate = "HARD_DUPLICATE";
}

public static class DuplicateCandidateDecisions
{
    public const string Candidate = "CANDIDATE";
    public const string Dismissed = "DISMISSED";
    public const string Confirmed = "CONFIRMED";
}

public sealed record DuplicateAssetRegistrationRequestDto(
    Guid UploadReceiptId,
    Guid PreUploadCheckId,
    Guid UserId,
    Guid? DeviceId,
    Guid? GroupNodeId,
    string BusinessObjectKey,
    string ExternalVideoId,
    string StorageKey,
    long SizeBytes,
    string ByteSha256,
    DateTimeOffset UploadedAtUtc);

public sealed record DuplicateCandidateDto(
    Guid CandidateId,
    Guid SourceVideoAssetId,
    Guid MatchedVideoAssetId,
    string MatchKind,
    string ReasonCode,
    string Decision,
    DateTimeOffset DetectedAtUtc);

public sealed record DuplicateAssetRegistrationResponseDto(
    Guid VideoAssetId,
    bool Registered,
    IReadOnlyCollection<DuplicateCandidateDto> Candidates,
    DateTimeOffset RegisteredAtUtc);

public sealed record DuplicateCandidateLookupResponseDto(
    Guid VideoAssetId,
    IReadOnlyCollection<DuplicateCandidateDto> Candidates);