namespace App.Mobile.Android.Services.Stubs;

internal sealed class StubMobileOutboxService :
    global::App.Mobile.Android.Services.Abstractions.IMobileOutboxService,
    IDisposable
{
    private static readonly Action<global::Microsoft.Extensions.Logging.ILogger, string, Exception?> LogMediaDraftEnqueued =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(
            global::Microsoft.Extensions.Logging.LogLevel.Information,
            new global::Microsoft.Extensions.Logging.EventId(1000, nameof(LogMediaDraftEnqueued)),
            "Enqueued local pending sync media draft item {ItemId}");

    private static readonly Action<global::Microsoft.Extensions.Logging.ILogger, string, Exception?> LogEnqueued =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(
            global::Microsoft.Extensions.Logging.LogLevel.Information,
            new global::Microsoft.Extensions.Logging.EventId(1001, nameof(LogEnqueued)),
            "Enqueued local pending sync stub item {ItemId}");

    private static readonly Action<global::Microsoft.Extensions.Logging.ILogger, string, Exception?> LogRetried =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(
            global::Microsoft.Extensions.Logging.LogLevel.Information,
            new global::Microsoft.Extensions.Logging.EventId(1002, nameof(LogRetried)),
            "Retried local pending sync stub item {ItemId}");

    private static readonly Action<global::Microsoft.Extensions.Logging.ILogger, string, Exception?> LogRemoved =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(
            global::Microsoft.Extensions.Logging.LogLevel.Information,
            new global::Microsoft.Extensions.Logging.EventId(1003, nameof(LogRemoved)),
            "Removed local pending sync stub item {ItemId}");

    private static readonly Action<global::Microsoft.Extensions.Logging.ILogger, string, Exception?> LogRepaired =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(
            global::Microsoft.Extensions.Logging.LogLevel.Information,
            new global::Microsoft.Extensions.Logging.EventId(1004, nameof(LogRepaired)),
            "Repaired local pending sync media draft item {ItemId}");

    private static readonly Action<global::Microsoft.Extensions.Logging.ILogger, string, Exception?> LogReportDraftEnqueued =
        global::Microsoft.Extensions.Logging.LoggerMessage.Define<string>(
            global::Microsoft.Extensions.Logging.LogLevel.Information,
            new global::Microsoft.Extensions.Logging.EventId(1005, nameof(LogReportDraftEnqueued)),
            "Enqueued local pending sync report draft item {ItemId}");

    private readonly object _gate = new();
    private readonly SemaphoreSlim _snapshotLoadGate = new(1, 1);
    private readonly List<global::App.Mobile.Android.Outbox.PendingSyncItem> _items = [];
    private readonly Dictionary<string, Func<Task<global::System.IO.Stream>>> _itemReadFactories = [];
    private readonly global::Microsoft.Extensions.Logging.ILogger<StubMobileOutboxService> _logger;
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftValidationService _reportDraftValidationService;
    private readonly global::App.Mobile.Android.Services.Abstractions.ILocalDuplicatePrecheckService _duplicatePrecheckService;
    private readonly global::App.Mobile.Android.Services.Abstractions.ILocalMediaDraftRepairService _draftRepairService;
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileSelectedMediaStore _selectedMediaStore;
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileOutboxSnapshotStore _snapshotStore;
    private bool _snapshotLoaded;
    private int _sequence;

    public StubMobileOutboxService(
        global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftValidationService reportDraftValidationService,
        global::App.Mobile.Android.Services.Abstractions.ILocalDuplicatePrecheckService duplicatePrecheckService,
        global::App.Mobile.Android.Services.Abstractions.ILocalMediaDraftRepairService draftRepairService,
        global::App.Mobile.Android.Services.Abstractions.IMobileSelectedMediaStore selectedMediaStore,
        global::App.Mobile.Android.Services.Abstractions.IMobileOutboxSnapshotStore snapshotStore,
        global::Microsoft.Extensions.Logging.ILogger<StubMobileOutboxService> logger)
    {
        _reportDraftValidationService = reportDraftValidationService;
        _duplicatePrecheckService = duplicatePrecheckService;
        _draftRepairService = draftRepairService;
        _selectedMediaStore = selectedMediaStore;
        _snapshotStore = snapshotStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<global::App.Mobile.Android.Outbox.PendingSyncItem>> GetItemsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        lock (_gate)
        {
            IReadOnlyList<global::App.Mobile.Android.Outbox.PendingSyncItem> items =
            [
                .. _items.OrderByDescending(item => item.CreatedAtUtc)
            ];

            return items;
        }
    }

    public async Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueCurrentSelectionAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        var currentSelection = await _selectedMediaStore.GetCurrentAsync(cancellationToken);
        var precheckResult = _duplicatePrecheckService.CheckAgainstOutbox(currentSelection, GetItemsSnapshot());
        if (!precheckResult.CanEnqueue)
        {
            return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                Applied: false,
                Message: precheckResult.Message,
                Item: null);
        }

        var currentSelectionEntry = await _selectedMediaStore.TakeCurrentAsync(cancellationToken);
        if (currentSelectionEntry is null)
        {
            return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                Applied: false,
                Message: currentSelection is null
                    ? global::App.Mobile.Android.Localization.MobileUiText.PendingSyncNoCurrentSelectionText
                    : global::App.Mobile.Android.Localization.MobileUiText.PendingSyncReselectAfterRestartText,
                Item: null);
        }

        global::App.Mobile.Android.Outbox.PendingSyncItem item;

        lock (_gate)
        {
            _sequence++;

            item = new global::App.Mobile.Android.Outbox.PendingSyncItem(
                ItemId: $"pending-sync-{_sequence}",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Title: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncMediaDraftTitle(
                    currentSelectionEntry.Descriptor.FileName),
                SummaryText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncMediaDraftSummary,
                Status: global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued,
                LastActionText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncMediaDraftEnqueuedLastAction,
                LocalMediaDraft: new global::App.Mobile.Android.Outbox.PendingSyncItemLocalMediaDraft(
                    CacheKey: currentSelectionEntry.Descriptor.CacheKey,
                    Source: currentSelectionEntry.Descriptor.Source,
                    FileName: currentSelectionEntry.Descriptor.FileName,
                    ContentType: currentSelectionEntry.Descriptor.ContentType,
                    SelectedAtUtc: currentSelectionEntry.Descriptor.SelectedAtUtc,
                    HasLocalReadHandle: currentSelectionEntry.Descriptor.HasLocalReadHandle));

            _items.Insert(0, item);
            _itemReadFactories[item.ItemId] = currentSelectionEntry.OpenReadAsync;
        }

        await SaveSnapshotAsync(cancellationToken);

        LogMediaDraftEnqueued(_logger, item.ItemId, null);

        return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
            Applied: true,
            Message: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncMediaDraftHandoffResultText(
                item.LocalMediaDraft!.FileName),
            Item: item);
    }

    public Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueStubItemAsync(
        CancellationToken cancellationToken = default)
    {
        return EnqueueStubItemCoreAsync(cancellationToken);
    }

    public async Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueReportDraftAsync(
        global::App.Mobile.Android.Reports.MobileReportDraft draft,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        var validationResult = _reportDraftValidationService.ValidateForLocalQueue(draft);
        if (!validationResult.IsValidForLocalQueue)
        {
            var blockedMessage = global::App.Mobile.Android.Localization.MobileUiText
                .GetReportDraftQueueBlockedByValidationText();

            return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                Applied: false,
                Message: $"{blockedMessage} {validationResult.SummaryText}",
                Item: null);
        }

        lock (_gate)
        {
            if (_items.Any(item =>
                    item.LocalReportDraft is not null
                    && string.Equals(item.LocalReportDraft.DraftId, draft.DraftId, StringComparison.Ordinal)))
            {
                return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftAlreadyQueuedText,
                    Item: null);
            }
        }

        global::App.Mobile.Android.Outbox.PendingSyncItem item;

        lock (_gate)
        {
            _sequence++;

            item = new global::App.Mobile.Android.Outbox.PendingSyncItem(
                ItemId: $"pending-sync-{_sequence}",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Title: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncReportDraftTitle(draft.Title),
                SummaryText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncReportDraftSummary,
                Status: global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued,
                LastActionText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncReportDraftEnqueuedLastAction,
                LocalMediaDraft: null,
                LocalReportDraft: new global::App.Mobile.Android.Outbox.PendingSyncItemLocalReportDraft(
                    DraftId: draft.DraftId,
                    Title: draft.Title,
                    CreatedAtUtc: draft.CreatedAtUtc,
                    UpdatedAtUtc: draft.UpdatedAtUtc,
                    AttachmentCount: draft.Attachments.Count,
                    VideoAttachmentCount: draft.Attachments.Count(attachment =>
                        attachment.Kind == global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video),
                    HasLocalAttachments: draft.Attachments.Count > 0));

            _items.Insert(0, item);
        }

        await SaveSnapshotAsync(cancellationToken);

        LogReportDraftEnqueued(_logger, item.ItemId, null);

        return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
            Applied: true,
            Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftQueuedLocalText(draft.Title),
            Item: item);
    }

    public async Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> RepairLocalMediaDraftAsync(
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        var targetItem = FindItem(itemId);
        if (targetItem is null)
        {
            return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                Applied: false,
                Message: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncItemNotFoundText,
                Item: null);
        }

        var currentSelection = await _selectedMediaStore.GetCurrentAsync(cancellationToken);
        var repairCheck = _draftRepairService.CheckCurrentSelectionForDraftRepair(currentSelection, targetItem);
        if (!repairCheck.CanApply)
        {
            return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                Applied: false,
                Message: repairCheck.Message,
                Item: targetItem);
        }

        var currentSelectionEntry = await _selectedMediaStore.TakeCurrentAsync(cancellationToken);
        if (currentSelectionEntry is null)
        {
            return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                Applied: false,
                Message: global::App.Mobile.Android.Localization.MobileUiText.QueueRepairSelectionHasNoLiveHandleText,
                Item: targetItem);
        }

        global::App.Mobile.Android.Outbox.PendingSyncItem? updatedItem = null;

        lock (_gate)
        {
            var index = _items.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncItemNotFoundText,
                    Item: null);
            }

            updatedItem = _items[index] with
            {
                LastActionText = global::App.Mobile.Android.Localization.MobileUiText.QueueRepairSuccessLastActionText,
                LocalMediaDraft = new global::App.Mobile.Android.Outbox.PendingSyncItemLocalMediaDraft(
                    CacheKey: currentSelectionEntry.Descriptor.CacheKey,
                    Source: currentSelectionEntry.Descriptor.Source,
                    FileName: currentSelectionEntry.Descriptor.FileName,
                    ContentType: currentSelectionEntry.Descriptor.ContentType,
                    SelectedAtUtc: currentSelectionEntry.Descriptor.SelectedAtUtc,
                    HasLocalReadHandle: true)
            };

            _items[index] = updatedItem;
            _itemReadFactories[itemId] = currentSelectionEntry.OpenReadAsync;
        }

        await SaveSnapshotAsync(cancellationToken);

        LogRepaired(_logger, itemId, null);

        return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
            Applied: true,
            Message: global::App.Mobile.Android.Localization.MobileUiText.GetQueueRepairSuccessText(
                updatedItem!.LocalMediaDraft!.FileName),
            Item: updatedItem);
    }

    private async Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> EnqueueStubItemCoreAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        global::App.Mobile.Android.Outbox.PendingSyncItem item;

        lock (_gate)
        {
            _sequence++;

            item = new global::App.Mobile.Android.Outbox.PendingSyncItem(
                ItemId: $"pending-sync-{_sequence}",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Title: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncItemTitle(_sequence),
                SummaryText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncItemSummary,
                Status: global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued,
                LastActionText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncEnqueuedLastAction,
                LocalMediaDraft: null);

            _items.Insert(0, item);
        }

        await SaveSnapshotAsync(cancellationToken);

        LogEnqueued(_logger, item.ItemId, null);

        return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
            Applied: true,
            Message: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncEnqueueResultText(item.Title),
            Item: item);
    }

    public async Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> RetryAsync(
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        global::App.Mobile.Android.Outbox.PendingSyncItem? updatedItem = null;

        lock (_gate)
        {
            var index = _items.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncItemNotFoundText,
                    Item: null);
            }

            updatedItem = _items[index] with
            {
                Status = global::App.Mobile.Android.Outbox.PendingSyncItemStatus.RetryRequested,
                LastActionText = global::App.Mobile.Android.Localization.MobileUiText.PendingSyncRetriedLastAction
            };

            _items[index] = updatedItem;
        }

        await SaveSnapshotAsync(cancellationToken);

        LogRetried(_logger, updatedItem!.ItemId, null);

        return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
            Applied: true,
            Message: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncRetryResultText(updatedItem.Title),
            Item: updatedItem);
    }

    public async Task<global::App.Mobile.Android.Outbox.PendingSyncOperationResult> RemoveAsync(
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        global::App.Mobile.Android.Outbox.PendingSyncItem? removedItem = null;

        lock (_gate)
        {
            var index = _items.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncItemNotFoundText,
                    Item: null);
            }

            removedItem = _items[index];
            _items.RemoveAt(index);
            _itemReadFactories.Remove(removedItem.ItemId);
        }

        await SaveSnapshotAsync(cancellationToken);

        LogRemoved(_logger, removedItem!.ItemId, null);

        return new global::App.Mobile.Android.Outbox.PendingSyncOperationResult(
            Applied: true,
            Message: global::App.Mobile.Android.Localization.MobileUiText.GetPendingSyncRemoveResultText(removedItem.Title),
            Item: removedItem);
    }

    private IReadOnlyList<global::App.Mobile.Android.Outbox.PendingSyncItem> GetItemsSnapshot()
    {
        lock (_gate)
        {
            return
            [
                .. _items
            ];
        }
    }

    private async Task EnsureSnapshotLoadedAsync(CancellationToken cancellationToken)
    {
        if (_snapshotLoaded)
        {
            return;
        }

        await _snapshotLoadGate.WaitAsync(cancellationToken);
        try
        {
            if (_snapshotLoaded)
            {
                return;
            }

            var persistedItems = await _snapshotStore.LoadAsync(cancellationToken);
            var normalizedItems = persistedItems
                .Select(NormalizeRestoredItem)
                .ToList();

            lock (_gate)
            {
                _items.Clear();
                _items.AddRange(normalizedItems);
                _itemReadFactories.Clear();
                _sequence = _items
                    .Select(item => TryParseSequence(item.ItemId))
                    .DefaultIfEmpty(0)
                    .Max();
                _snapshotLoaded = true;
            }
        }
        finally
        {
            _snapshotLoadGate.Release();
        }
    }

    private async Task SaveSnapshotAsync(CancellationToken cancellationToken)
    {
        await _snapshotStore.SaveAsync(GetItemsSnapshot(), cancellationToken);
    }

    private global::App.Mobile.Android.Outbox.PendingSyncItem? FindItem(string itemId)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(item => item.ItemId == itemId);
        }
    }

    private static global::App.Mobile.Android.Outbox.PendingSyncItem NormalizeRestoredItem(
        global::App.Mobile.Android.Outbox.PendingSyncItem item)
    {
        if (item.LocalMediaDraft is null)
        {
            return item;
        }

        return item with
        {
            LocalMediaDraft = item.LocalMediaDraft with
            {
                HasLocalReadHandle = false
            },
            LastActionText = global::App.Mobile.Android.Localization.MobileUiText.PendingSyncRestoredMetadataLastActionText
        };
    }

    private static int TryParseSequence(string itemId)
    {
        var suffixIndex = itemId.LastIndexOf('-');
        if (suffixIndex < 0 || suffixIndex == itemId.Length - 1)
        {
            return 0;
        }

        return int.TryParse(itemId[(suffixIndex + 1)..], out var sequence)
            ? sequence
            : 0;
    }

    public void Dispose()
    {
        _snapshotLoadGate.Dispose();
        GC.SuppressFinalize(this);
    }
}