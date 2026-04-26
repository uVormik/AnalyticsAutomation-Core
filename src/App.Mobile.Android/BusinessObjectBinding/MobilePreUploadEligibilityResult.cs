namespace App.Mobile.Android.BusinessObjectBinding;

internal sealed record MobilePreUploadEligibilityResult(
    bool CanRunProductionPreUploadCheck,
    string MessageText,
    string RequiredActionText,
    global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot BindingSnapshot);