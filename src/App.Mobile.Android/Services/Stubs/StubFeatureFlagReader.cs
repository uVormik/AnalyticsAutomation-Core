namespace App.Mobile.Android.Services.Stubs;

internal sealed class StubFeatureFlagReader :
    global::App.Mobile.Android.Services.Abstractions.IFeatureFlagReader
{
    public const string ShellBannerFlag = "mobile.shell.banner";
    public const string QueueFoundationCardFlag = "mobile.queue.foundation-card";
    public const string BusinessObjectBindingBlockerCardFlag = "mobile.business-object-binding.blocker-card";
    public const string ReportDraftShellFlag = "mobile.report-draft.shell";
    public const string ReportFirstMediaAttachmentFlag = "mobile.report-first.media-attachment";

    public bool IsEnabled(string flagName)
    {
        return string.Equals(flagName, ShellBannerFlag, global::System.StringComparison.Ordinal)
            || string.Equals(flagName, QueueFoundationCardFlag, global::System.StringComparison.Ordinal)
            || string.Equals(flagName, BusinessObjectBindingBlockerCardFlag, global::System.StringComparison.Ordinal)
            || string.Equals(flagName, ReportDraftShellFlag, global::System.StringComparison.Ordinal)
            || string.Equals(flagName, ReportFirstMediaAttachmentFlag, global::System.StringComparison.Ordinal);
    }
}