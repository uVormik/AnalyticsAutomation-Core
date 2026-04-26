namespace App.Mobile.Android.Services.Local;

internal sealed class UnresolvedMobileBusinessObjectBindingService :
    global::App.Mobile.Android.Services.Abstractions.IMobileBusinessObjectBindingService
{
    private readonly object _gate = new();
    private global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectDraftIntent? _localIntent;

    public Task<global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(CreateSnapshot(_localIntent));
        }
    }

    public Task<global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot> CreateOrUpdateLocalIntentAsync(
        string? displayTitle,
        string? noteText,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var normalizedTitle = string.IsNullOrWhiteSpace(displayTitle)
            ? global::App.Mobile.Android.Localization.MobileUiText.BusinessObjectBindingDefaultLocalIntentTitle
            : displayTitle.Trim();
        var normalizedNote = string.IsNullOrWhiteSpace(noteText)
            ? string.Empty
            : noteText.Trim();

        lock (_gate)
        {
            _localIntent = _localIntent is null
                ? new global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectDraftIntent(
                    LocalIntentId: $"local-intent-{Guid.NewGuid():N}",
                    CreatedAtUtc: now,
                    UpdatedAtUtc: now,
                    DisplayTitle: normalizedTitle,
                    NoteText: normalizedNote,
                    IsLocalOnly: true,
                    ApprovalState: global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.Unresolved)
                : _localIntent with
                {
                    UpdatedAtUtc = now,
                    DisplayTitle = normalizedTitle,
                    NoteText = normalizedNote,
                    IsLocalOnly = true,
                    ApprovalState = global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.Unresolved
                };

            return Task.FromResult(CreateSnapshot(_localIntent));
        }
    }

    public Task<global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot> ClearLocalIntentAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _localIntent = null;
            return Task.FromResult(CreateSnapshot(_localIntent));
        }
    }

    private static global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot CreateSnapshot(
        global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectDraftIntent? localIntent)
    {
        return new global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingSnapshot(
            approvalState: global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.Unresolved,
            localIntent: localIntent,
            approvedBusinessObjectKey: null,
            messageText: global::App.Mobile.Android.Localization.MobileUiText.BusinessObjectBindingUnresolvedWarning,
            sourceDescriptionText: global::App.Mobile.Android.Localization.MobileUiText.BusinessObjectBindingSourceDescription);
    }
}