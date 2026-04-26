namespace App.Mobile.Android.Outbox;

internal sealed record PendingSyncItemLocalReportDraft(
    string DraftId,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int AttachmentCount,
    int VideoAttachmentCount,
    bool HasLocalAttachments);