namespace App.Mobile.Android.BusinessObjectBinding;

internal sealed record MobileBusinessObjectBindingSnapshot
{
    public MobileBusinessObjectBindingSnapshot(
        global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState approvalState,
        global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectDraftIntent? localIntent,
        string? approvedBusinessObjectKey,
        string messageText,
        string sourceDescriptionText)
    {
        if (approvalState is not global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.ApprovedResolved
            && !string.IsNullOrWhiteSpace(approvedBusinessObjectKey))
        {
            throw new ArgumentException(
                "Approved businessObjectKey is allowed only after approved binding is resolved.",
                nameof(approvedBusinessObjectKey));
        }

        ApprovalState = approvalState;
        LocalIntent = localIntent;
        ApprovedBusinessObjectKey = string.IsNullOrWhiteSpace(approvedBusinessObjectKey)
            ? null
            : approvedBusinessObjectKey;
        MessageText = messageText;
        SourceDescriptionText = sourceDescriptionText;
    }

    public global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState ApprovalState { get; }

    public global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectDraftIntent? LocalIntent { get; }

    public string? ApprovedBusinessObjectKey { get; }

    public string MessageText { get; }

    public string SourceDescriptionText { get; }
}