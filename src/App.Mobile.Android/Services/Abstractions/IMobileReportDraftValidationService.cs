namespace App.Mobile.Android.Services.Abstractions;

internal interface IMobileReportDraftValidationService
{
    global::App.Mobile.Android.Reports.Validation.MobileReportDraftValidationResult ValidateForLocalQueue(
        global::App.Mobile.Android.Reports.MobileReportDraft? draft);
}