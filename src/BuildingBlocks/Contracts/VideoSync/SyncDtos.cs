using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.VideoSync;

public enum PendingSyncItemType
{
    Unknown = 0,
    UploadReceipt = 1,
    DownloadReceipt = 2
}

public enum PendingSyncItemStatus
{
    Pending = 0,
    InProgress = 1,
    Failed = 2,
    Completed = 3
}

public sealed record PendingSyncItemDto(
    Guid PendingSyncItemId,
    PendingSyncItemType ItemType,
    PendingSyncItemStatus Status,
    Guid? DeviceId,
    DateTimeOffset CreatedAtUtc,
    int AttemptCount,
    string? LastErrorCode);

public sealed record PendingSyncSummaryDto(
    int TotalPending,
    int UploadReceiptsPending,
    int DownloadReceiptsPending,
    int FailedItems,
    DateTimeOffset? OldestPendingAtUtc);

public sealed record SyncPushRequestDto(
    Guid? DeviceId,
    IReadOnlyCollection<Guid> PendingItemIds);

public sealed record SyncPushResponseDto(
    int AcceptedCount,
    int RejectedCount,
    IReadOnlyCollection<Guid> AcceptedPendingItemIds);