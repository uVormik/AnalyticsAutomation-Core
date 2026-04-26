namespace App.Mobile.Android.Services.Abstractions;

internal interface IMobileReportDraftSnapshotStore
{
    Task<IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft>> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft> drafts,
        CancellationToken cancellationToken = default);
}