using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.VideoDownload;

public static class DownloadIntentV1Statuses
{
    public const string Created = "created";
    public const string Consumed = "consumed";
    public const string Expired = "expired";
    public const string Rejected = "rejected";
}

public static class DownloadReceiptV1Statuses
{
    public const string Accepted = "accepted";
    public const string DuplicateIgnored = "duplicate_ignored";
}

public sealed record CreateDownloadIntentRequestV1Dto(
    Guid UserId,
    Guid? DeviceId,
    Guid GroupNodeId,
    Guid VideoAssetId,
    string ExternalVideoId,
    string BusinessObjectKey,
    int? ExpiresInSeconds);

public sealed record CreateDownloadIntentResponseV1Dto(
    Guid DownloadIntentId,
    Guid UserId,
    Guid? DeviceId,
    Guid GroupNodeId,
    Guid VideoAssetId,
    string ExternalVideoId,
    string BusinessObjectKey,
    string Status,
    string SiteProvider,
    string DownloadUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record DownloadReceiptRequestV1Dto(
    Guid DownloadIntentId,
    Guid UserId,
    Guid? DeviceId,
    string ExternalVideoId,
    string? ClientReceiptKey,
    long? DownloadedBytes,
    DateTimeOffset DownloadedAtUtc);

public sealed record DownloadReceiptResponseV1Dto(
    Guid DownloadReceiptId,
    Guid DownloadIntentId,
    string Status,
    bool DuplicateIgnored,
    DateTimeOffset AcceptedAtUtc);

public sealed record DownloadIntentSummaryV1Dto(
    Guid DownloadIntentId,
    Guid UserId,
    Guid? DeviceId,
    Guid GroupNodeId,
    Guid VideoAssetId,
    string ExternalVideoId,
    string BusinessObjectKey,
    string Status,
    string SiteProvider,
    string DownloadUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ConsumedAtUtc,
    string? RejectedReason);

public sealed record DownloadReceiptSummaryV1Dto(
    Guid DownloadReceiptId,
    Guid DownloadIntentId,
    Guid UserId,
    Guid? DeviceId,
    string ExternalVideoId,
    string? ClientReceiptKey,
    long? DownloadedBytes,
    string Status,
    DateTimeOffset DownloadedAtUtc,
    DateTimeOffset AcceptedAtUtc);

public sealed record DownloadControlPlaneStatusV1Dto(
    IReadOnlyCollection<string> IntentStatuses,
    IReadOnlyCollection<string> ReceiptStatuses);