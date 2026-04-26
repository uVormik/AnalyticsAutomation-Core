namespace App.Mobile.Android.Services.Abstractions;

internal interface IMobileBusinessObjectBindingService
{
    Task<global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot> CreateOrUpdateLocalIntentAsync(
        string? displayTitle,
        string? noteText,
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot> ClearLocalIntentAsync(
        CancellationToken cancellationToken = default);
}