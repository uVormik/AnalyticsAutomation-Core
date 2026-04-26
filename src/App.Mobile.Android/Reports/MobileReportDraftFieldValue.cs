namespace App.Mobile.Android.Reports;

internal sealed record MobileReportDraftFieldValue(
    string FieldKey,
    string Label,
    string ValueText,
    bool IsRequired,
    bool IsPlaceholder);