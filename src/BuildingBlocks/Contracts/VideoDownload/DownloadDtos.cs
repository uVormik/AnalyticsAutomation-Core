using System;

namespace BuildingBlocks.Contracts.VideoDownload;

public sealed record CreateDownloadIntentRequestDto(
    Guid VideoAssetId,
    Guid? DeviceId,
    string? Reason);

public sealed record CreateDownloadIntentResponseDto(
    Guid DownloadIntentId,
    string SiteVideoId,
    string? DownloadUrl,
    string? DownloadToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record DownloadReceiptDto(
    Guid DownloadIntentId,
    Guid VideoAssetId,
    Guid? DeviceId,
    DateTimeOffset DownloadedAtUtc,
    bool Succeeded);

public sealed record DownloadReceiptAcceptedDto(
    Guid ReceiptId);