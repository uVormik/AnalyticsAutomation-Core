namespace App.Mobile.Android.Services.Abstractions;

internal interface IMobilePreUploadEligibilityGate
{
    Task<global::App.Mobile.Android.BusinessObjectBinding.MobilePreUploadEligibilityResult> EvaluateAsync(
        CancellationToken cancellationToken = default);
}