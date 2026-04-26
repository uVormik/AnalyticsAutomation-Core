namespace App.Mobile.Android.Reports;

internal sealed record MobileReportDraftOperationResult(
    bool Applied,
    string Message,
    MobileReportDraft? Draft,
    MobileReportAttachment? Attachment)
{
    public string? FieldKey { get; init; }
}