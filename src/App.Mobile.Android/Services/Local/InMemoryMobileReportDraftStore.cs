namespace App.Mobile.Android.Services.Local;

internal sealed class InMemoryMobileReportDraftStore :
    global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftStore
{
    private readonly object _gate = new();
    private readonly global::App.Mobile.Android.Services.Abstractions.IMobileLookupCatalogProvider _lookupCatalogProvider;
    private readonly List<global::App.Mobile.Android.Reports.MobileReportDraft> _drafts = [];
    private int _sequence;

    public InMemoryMobileReportDraftStore(
        global::App.Mobile.Android.Services.Abstractions.IMobileLookupCatalogProvider lookupCatalogProvider)
    {
        _lookupCatalogProvider = lookupCatalogProvider;
    }

    public Task<IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft>> GetDraftsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft>>(
                _drafts
                    .OrderByDescending(draft => draft.UpdatedAtUtc)
                    .ToArray());
        }
    }

    public Task<global::App.Mobile.Android.Reports.MobileReportDraft?> GetDraftAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);

        lock (_gate)
        {
            return Task.FromResult(
                _drafts.FirstOrDefault(draft =>
                    string.Equals(draft.DraftId, draftId, StringComparison.Ordinal)));
        }
    }

    public async Task<global::App.Mobile.Android.Reports.MobileReportDraft> CreateFpvDraftAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await _lookupCatalogProvider.GetReportDraftFieldsAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var sequence = Interlocked.Increment(ref _sequence);
        var fields = snapshot.Fields
            .Select(field => new global::App.Mobile.Android.Reports.MobileReportDraftFieldValue(
                FieldKey: field.FieldKey,
                Label: field.Label,
                ValueText: field.PlaceholderText,
                IsRequired: field.IsRequired,
                IsPlaceholder: true))
            .ToArray();

        var draft = new global::App.Mobile.Android.Reports.MobileReportDraft(
            DraftId: Guid.NewGuid().ToString("N"),
            CreatedAtUtc: now,
            UpdatedAtUtc: now,
            Title: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftTitle(sequence),
            Status: global::App.Mobile.Android.Reports.MobileReportDraftStatus.Draft,
            Fields: fields,
            Attachments: Array.Empty<global::App.Mobile.Android.Reports.MobileReportAttachment>());

        lock (_gate)
        {
            _drafts.Insert(0, draft);
        }

        return draft;
    }

    public Task<global::App.Mobile.Android.Reports.MobileReportDraftOperationResult> AttachSelectedVideoAsync(
        string draftId,
        global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);

        if (descriptor is null || !IsValidDescriptor(descriptor))
        {
            return Task.FromResult(
                new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: false,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftInvalidVideoSelectionText,
                    Draft: null,
                    Attachment: null));
        }

        lock (_gate)
        {
            var index = _drafts.FindIndex(draft =>
                string.Equals(draft.DraftId, draftId, StringComparison.Ordinal));

            if (index < 0)
            {
                return Task.FromResult(
                    new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                        Applied: false,
                        Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftNotFoundMessage,
                        Draft: null,
                        Attachment: null));
            }

            var currentDraft = _drafts[index];
            if (HasDuplicateVideoAttachment(currentDraft, descriptor))
            {
                return Task.FromResult(
                    new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                        Applied: false,
                        Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftDuplicateAttachmentWarningText,
                        Draft: currentDraft,
                        Attachment: null));
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

            return Task.FromResult(
                new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: true,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftAttachVideoSuccessText(
                        descriptor.FileName),
                    Draft: updatedDraft,
                    Attachment: attachment));
        }
    }

    public Task<global::App.Mobile.Android.Reports.MobileReportDraftOperationResult> MarkDraftQueuedLocalAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(draftId);

        lock (_gate)
        {
            var index = _drafts.FindIndex(draft =>
                string.Equals(draft.DraftId, draftId, StringComparison.Ordinal));

            if (index < 0)
            {
                return Task.FromResult(
                    new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                        Applied: false,
                        Message: global::App.Mobile.Android.Localization.MobileUiText.ReportDraftNotFoundMessage,
                        Draft: null,
                        Attachment: null));
            }

            var updatedDraft = _drafts[index] with
            {
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                Status = global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal
            };

            _drafts[index] = updatedDraft;

            return Task.FromResult(
                new global::App.Mobile.Android.Reports.MobileReportDraftOperationResult(
                    Applied: true,
                    Message: global::App.Mobile.Android.Localization.MobileUiText.GetReportDraftQueuedLocalText(
                        updatedDraft.Title),
                    Draft: updatedDraft,
                    Attachment: null));
        }
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
}