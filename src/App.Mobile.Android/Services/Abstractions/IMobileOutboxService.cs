namespace App.Mobile.Android.Services.Abstractions;

internal interface IMobileOutboxService
{
    Task<IReadOnlyList<global::App.Mobile.Android.Outbox.PendingSyncItem>> GetItemsAsync(
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueCurrentSelectionAsync(
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueStubItemAsync(
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueReportDraftAsync(
        global::App.Mobile.Android.Reports.MobileReportDraft draft,
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> RepairLocalMediaDraftAsync(
        string itemId,
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> RetryAsync(
        string itemId,
        CancellationToken cancellationToken = default);

    Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> RemoveAsync(
        string itemId,
        CancellationToken cancellationToken = default);
}