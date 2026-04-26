namespace App.Mobile.Android.Services.Local;

internal sealed class LocalMobilePreUploadEligibilityGate :
    global::App.Mobile.Android.Services.Abstractions.IMobilePreUploadEligibilityGate
{
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileBusinessObjectBindingService _bindingService;

    public LocalMobilePreUploadEligibilityGate(
        global::App.Mobile.Android.Services.Abstractions.IMobileBusinessObjectBindingService bindingService)
    {
        _bindingService = bindingService;
    }

    public async Task<global::App.Mobile.Android.BusinessObjectBinding.MobilePreUploadEligibilityResult> EvaluateAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _bindingService.GetCurrentAsync(cancellationToken);
        var canRun = snapshot.ApprovalState is global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.ApprovedResolved
            && !string.IsNullOrWhiteSpace(snapshot.ApprovedBusinessObjectKey);

        if (canRun)
        {
            return new global::App.Mobile.Android.BusinessObjectBinding.MobilePreUploadEligibilityResult(
                CanRunProductionPreUploadCheck: true,
                MessageText: global::App.Mobile.Android.Localization.MobileUiText.BusinessObjectBindingEligibleMessage,
                RequiredActionText: string.Empty,
                BindingSnapshot: snapshot);
        }

        return new global::App.Mobile.Android.BusinessObjectBinding.MobilePreUploadEligibilityResult(
            CanRunProductionPreUploadCheck: false,
            MessageText: global::App.Mobile.Android.Localization.MobileUiText.PreUploadCheckBlockedMessage,
            RequiredActionText: global::App.Mobile.Android.Localization.MobileUiText.BusinessObjectBindingRequiredActionText,
            BindingSnapshot: snapshot);
    }
}