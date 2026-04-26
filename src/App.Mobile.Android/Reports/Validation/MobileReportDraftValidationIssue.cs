namespace App.Mobile.Android.Reports.Validation;

internal sealed record MobileReportDraftValidationIssue(
    string IssueId,
    MobileReportDraftValidationSeverity Severity,
    string FieldKey,
    string Message);