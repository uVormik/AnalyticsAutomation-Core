namespace App.Mobile.Android.Services.Local;

internal sealed class LocalMobileReportDraftValidationService :
    global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftValidationService
{
    public global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationResult ValidateForLocalQueue(
        global::App.Mobile.Android.Reports.MobileReportDraft? draft)
    {
        List<global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationIssue> issues = [];

        if (draft is null)
        {
            issues.Add(
                new global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationIssue(
                    IssueId: "draft-not-found",
                    Severity: global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationSeverity.Error,
                    FieldKey: string.Empty,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftValidationDraftNotFoundText()));

            return new global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationResult(
                IsValidForLocalQueue: false,
                SummaryText: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftValidationNotReadySummaryText(),
                Issues: issues);
        }

        foreach (var field in draft.Fields.Where(field => field.IsRequired))
        {
            if (string.IsNullOrWhiteSpace(field.ValueText) || field.IsPlaceholder)
            {
                issues.Add(
                    new global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationIssue(
                        IssueId: $"required-{field.FieldKey}",
                        Severity: global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationSeverity.Error,
                        FieldKey: field.FieldKey,
                        Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftRequiredFieldMissingText(
                            field.Label)));
            }
        }

        var hasVideoAttachment = draft.Attachments.Any(attachment =>
            attachment.Kind == global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video);

        if (!hasVideoAttachment)
        {
            issues.Add(
                new global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationIssue(
                    IssueId: "video-attachment-required",
                    Severity: global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationSeverity.Error,
                    FieldKey: "attachments",
                    Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftValidationMissingVideoAttachmentText()));
        }

        return new global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationResult(
            IsValidForLocalQueue: issues.Count == 0,
            SummaryText: issues.Count == 0
                ? global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftValidationReadySummaryText()
                : global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftValidationNotReadySummaryText(),
            Issues: issues);
    }
}