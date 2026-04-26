namespace App.Mobile.Android.Reports.Validation;

internal sealed record MobileReportDraftValidationResult(
    bool IsValidForLocalQueue,
    string SummaryText,
    IReadOnlyList<MobileReportDraftValidationIssue> Issues);