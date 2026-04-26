namespace App.Mobile.Android.Services.Local;

internal sealed class InMemoryMobileReportDraftStore :
    global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftStore,
    IDisposable
{
    private static readonly HashSet<string> RequiredFieldKeys = new(StringComparer.Ordinal)
    {
        "device_type",
        "serial_number",
        "delivery_start",
        "delivery_time",
        "distance",
        "target_type",
        "reason"
    };

    private readonly object _gate = new();
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileLookupCatalogProvider _lookupCatalogProvider;
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftSnapshotStore _snapshotStore;
    private readonly List<global::App.Mobile.Android.Reports.MobileReportDraft> _drafts = [];
    private bool _snapshotLoaded;
    private int _sequence;

    public InMemoryMobileReportDraftStore(
        global::App.Mobile.Android.Services.Abstractions.IMobileLookupCatalogProvider lookupCatalogProvider,
        global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftSnapshotStore snapshotStore)
    {
        _lookupCatalogProvider = lookupCatalogProvider;
        _snapshotStore = snapshotStore;
    }

    public async Task<IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft>> GetDraftsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        lock (_gate)
        {
            return _drafts
                .OrderByDescending(draft => draft.UpdatedAtUtc)
                .ToArray();
        }
    }

    public async Task<global::App.Mobile.Android.Reports.MobileReportDraft?> GetDraftAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);
        await EnsureSnapshotLoadedAsync(cancellationToken);

        lock (_gate)
        {
            return _drafts.FirstOrDefault(draft =>
                string.Equals(draft.DraftId, draftId, StringComparison.Ordinal));
        }
    }

    public async Task<global::App.Mobile.Android.Reports.MobileReportDraft> CreateFpvDraftAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSnapshotLoadedAsync(cancellationToken);

        var snapshot = await _lookupCatalogProvider.GetReportDraftFieldsAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var sequence = Interlocked.Increment(ref _sequence);
        var fields = snapshot.Fields
            .Select(field => new global::App.Mobile.Android.Reports.MobileReportDraftFieldValue(
                FieldKey: field.FieldKey,
                Label: field.Label,
                ValueText: field.PlaceholderText,
                IsRequired: RequiredFieldKeys.Contains(field.FieldKey),
                IsPlaceholder: true)
            {
                SectionKey = field.SectionKey,
                FieldKind = field.Kind,
                SelectorMode = field.SelectorMode,
                LastUpdatedAtUtc = null
            })
            .ToArray();

        var draft = new global::App.Mobile.Android.Reports.MobileReportDraft(
            DraftId: Guid.NewGuid().ToString("N"),
            CreatedAtUtc: now,
            UpdatedAtUtc: now,
            Title: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftTitle(sequence),
            Status: global::App.Mobile.Android.Reports.MobileReportDraftStatus.Draft,
            Fields: fields,
            Attachments: Array.Empty<global::App.Mobile.Android.Reports.MobileReportAttachment>());

        global::App.Mobile.Android.Reports.MobileReportDraft[] persistedDrafts;
        lock (_gate)
        {
            _drafts.Insert(0, draft);
            persistedDrafts = _drafts.ToArray();
        }

        await _snapshotStore.SaveAsync(persistedDrafts, cancellationToken);
        return draft;
    }

    public async Task<global::App.Mobile.Android.Reports.MobileReportDraftOperationResult> AttachSelectedVideoAsync(
        string draftId,
        global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);
        await EnsureSnapshotLoadedAsync(cancellationToken);

        if (descriptor is null || !IsValidDescriptor(descriptor))
        {
            return new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                Applied: false,
                Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftInvalidVideoSelectionText,
                Draft: null,
                Attachment: null);
        }

        global::App.Mobile.Android.Reports.MobileReportDraftOperationResult operationResult;
        global::App.Mobile.Android.Reports.MobileReportDraft[]? persistedDrafts = null;

        lock (_gate)
        {
            var index = _drafts.FindIndex(draft =>
                string.Equals(draft.DraftId, draftId, StringComparison.Ordinal));

            if (index < 0)
            {
                return new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftNotFoundMessage,
                    Draft: null,
                    Attachment: null);
            }

            var currentDraft = _drafts[index];
            if (HasDuplicateVideoAttachment(currentDraft, descriptor))
            {
                return new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftDuplicateAttachmentWarningText,
                    Draft: currentDraft,
                    Attachment: null);
            }

            var now = DateTimeOffset.UtcNow;
            var attachment = new global::App.Mobile.Android.Reports.MobileReportAttachment(
                AttachmentId: Guid.NewGuid().ToString("N"),
                DraftId: currentDraft.DraftId,
                Kind: global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video,
                FileName: descriptor.FileName,
                ContentType: descriptor.ContentType,
                SourceText: global::App.Mobile.Android.Localization.MobileUiText.GetSelectedMediaSourceText(descriptor.Source),
                AddedAtUtc: now,
                SelectedMediaCacheKey: descriptor.CacheKey,
                HasLocalReadHandle: descriptor.HasLocalReadHandle);

            var updatedDraft = currentDraft with
            {
                UpdatedAtUtc = now,
                Status = currentDraft.Status == global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal
                    ? global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal
                    : global::App.Mobile.Android.Reports.MobileReportDraftStatus.ReadyForAttachmentReview,
                Attachments = currentDraft.Attachments
                    .Concat([attachment])
                    .ToArray()
            };

            _drafts[index] = updatedDraft;
            persistedDrafts = _drafts.ToArray();
            operationResult = new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                Applied: true,
                Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftAttachVideoSuccessText(
                    descriptor.FileName),
                Draft: updatedDraft,
                Attachment: attachment);
        }

        await _snapshotStore.SaveAsync(persistedDrafts, cancellationToken);
        return operationResult;
    }

    public async Task<global::App.Mobile.Android.Reports.MobileReportDraftOperationResult> UpdateFieldValueAsync(
        string draftId,
        string fieldKey,
        string? valueText,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);
        await EnsureSnapshotLoadedAsync(cancellationToken);

        global::App.Mobile.Android.Reports.MobileReportDraftOperationResult operationResult;
        global::App.Mobile.Android.Reports.MobileReportDraft[]? persistedDrafts = null;

        lock (_gate)
        {
            var draftIndex = _drafts.FindIndex(draft =>
                string.Equals(draft.DraftId, draftId, StringComparison.Ordinal));

            if (draftIndex < 0)
            {
                return new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftNotFoundMessage,
                    Draft: null,
                    Attachment: null)
                {
                    FieldKey = fieldKey
                };
            }

            var currentDraft = _drafts[draftIndex];
            var fieldIndex = currentDraft.Fields
                .Select((field, index) => new { field, index })
                .FirstOrDefault(entry =>
                    string.Equals(entry.field.FieldKey, fieldKey, StringComparison.Ordinal))
                ?.index ?? -1;

            if (fieldIndex < 0)
            {
                return new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftFieldNotFoundMessage,
                    Draft: currentDraft,
                    Attachment: null)
                {
                    FieldKey = fieldKey
                };
            }

            var now = DateTimeOffset.UtcNow;
            var currentField = currentDraft.Fields[fieldIndex];
            var normalizedValue = NormalizeFieldValue(valueText);
            var updatedField = currentField with
            {
                ValueText = normalizedValue,
                IsPlaceholder = false,
                LastUpdatedAtUtc = now
            };

            var updatedFields = currentDraft.Fields.ToArray();
            updatedFields[fieldIndex] = updatedField;

            var updatedDraft = currentDraft with
            {
                UpdatedAtUtc = now,
                Fields = updatedFields
            };

            _drafts[draftIndex] = updatedDraft;
            persistedDrafts = _drafts.ToArray();
            operationResult = new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                Applied: true,
                Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftFieldUpdatedText(
                    updatedField.Label,
                    normalizedValue),
                Draft: updatedDraft,
                Attachment: null)
            {
                FieldKey = updatedField.FieldKey
            };
        }

        await _snapshotStore.SaveAsync(persistedDrafts, cancellationToken);
        return operationResult;
    }

    public async Task<global::App.Mobile.Android.Reports.MobileReportDraftOperationResult> MarkDraftQueuedLocalAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);
        await EnsureSnapshotLoadedAsync(cancellationToken);

        global::App.Mobile.Android.Reports.MobileReportDraftOperationResult operationResult;
        global::App.Mobile.Android.Reports.MobileReportDraft[]? persistedDrafts = null;

        lock (_gate)
        {
            var index = _drafts.FindIndex(draft =>
                string.Equals(draft.DraftId, draftId, StringComparison.Ordinal));

            if (index < 0)
            {
                return new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftNotFoundMessage,
                    Draft: null,
                    Attachment: null);
            }

            var updatedDraft = _drafts[index] with
            {
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                Status = global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal
            };

            _drafts[index] = updatedDraft;
            persistedDrafts = _drafts.ToArray();
            operationResult = new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                Applied: true,
                Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftQueuedLocalText(
                    updatedDraft.Title),
                Draft: updatedDraft,
                Attachment: null);
        }

        await _snapshotStore.SaveAsync(persistedDrafts, cancellationToken);
        return operationResult;
    }

    private async Task EnsureSnapshotLoadedAsync(CancellationToken cancellationToken)
    {
        if (_snapshotLoaded)
        {
            return;
        }

        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            if (_snapshotLoaded)
            {
                return;
            }

            var restoredDrafts = await _snapshotStore.LoadAsync(cancellationToken);
            var normalizedDrafts = restoredDrafts
                .Select(NormalizeRestoredDraft)
                .ToArray();

            lock (_gate)
            {
                _drafts.Clear();
                _drafts.AddRange(normalizedDrafts);
                _sequence = Math.Max(_sequence, _drafts.Count);
                _snapshotLoaded = true;
            }
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private static global::App.Mobile.Android.Reports.MobileReportDraft NormalizeRestoredDraft(
        global::App.Mobile.Android.Reports.MobileReportDraft draft)
    {
        return draft with
        {
            IsRestoredFromSnapshot = true,
            Fields = draft.Fields.ToArray(),
            Attachments = draft.Attachments
                .Select(attachment => attachment with { HasLocalReadHandle = false })
                .ToArray()
        };
    }

    private static bool IsValidDescriptor(global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor descriptor)
    {
        return !string.IsNullOrWhiteSpace(descriptor.CacheKey)
            && !string.IsNullOrWhiteSpace(descriptor.FileName);
    }

    private static bool HasDuplicateVideoAttachment(
        global::App.Mobile.Android.Reports.MobileReportDraft draft,
        global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor descriptor)
    {
        return draft.Attachments.Any(attachment =>
            attachment.Kind == global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video
            && (
                string.Equals(attachment.SelectedMediaCacheKey, descriptor.CacheKey, StringComparison.Ordinal)
                || (
                    string.Equals(attachment.FileName, descriptor.FileName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        NormalizeContentType(attachment.ContentType),
                        NormalizeContentType(descriptor.ContentType),
                        StringComparison.Ordinal))
            ));
    }

    private static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? string.Empty
            : contentType.Trim().ToLowerInvariant();
    }

    private static string NormalizeFieldValue(string? valueText)
    {
        return string.IsNullOrWhiteSpace(valueText)
            ? string.Empty
            : valueText.Trim();
    }
    public void Dispose()
    {
        _loadGate.Dispose();
    }
}