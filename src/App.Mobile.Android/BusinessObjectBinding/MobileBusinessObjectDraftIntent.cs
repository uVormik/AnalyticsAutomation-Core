namespace App.Mobile.Android.BusinessObjectBinding;

internal sealed record MobileBusinessObjectDraftIntent(
    string LocalIntentId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string DisplayTitle,
    string NoteText,
    bool IsLocalOnly,
    global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState ApprovalState);