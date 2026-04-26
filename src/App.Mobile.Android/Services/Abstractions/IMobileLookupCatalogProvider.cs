namespace App.Mobile.Android.Services.Abstractions;

internal interface IMobileLookupCatalogProvider
{
    Task<global::App.Mobile.Android.Lookup.MobileLookupCatalogSnapshot> GetReportDraftFieldsAsync(
        CancellationToken cancellationToken = default);
}