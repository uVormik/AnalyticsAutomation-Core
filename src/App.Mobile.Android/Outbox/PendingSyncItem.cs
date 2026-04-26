namespace App.Mobile.Android.Outbox;

internal sealed record PendingSyncItem(
    string ItemId,
    DateTimeOffset CreatedAtUtc,
    string Title,
    string SummaryText,
    PendingSyncItemStatus Status,
    string? LastActionText,
    PendingSyncItemLocalMediaDraft? LocalMediaDraft = null,
    PendingSyncItemLocalReportDraft? LocalReportDraft = null);