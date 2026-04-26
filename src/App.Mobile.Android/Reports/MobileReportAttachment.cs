namespace App.Mobile.Android.Reports;

internal sealed record MobileReportAttachment(
    string AttachmentId,
    string DraftId,
    MobileReportAttachmentKind Kind,
    string FileName,
    string? ContentType,
    string SourceText,
    DateTimeOffset AddedAtUtc,
    string SelectedMediaCacheKey,
    bool HasLocalReadHandle);