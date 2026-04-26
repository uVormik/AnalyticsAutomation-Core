namespace App.Mobile.Android.Reports;

internal sealed record MobileReportDraft(
    string DraftId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string Title,
    MobileReportDraftStatus Status,
    IReadOnlyList<MobileReportDraftFieldValue> Fields,
    IReadOnlyList<MobileReportAttachment> Attachments);