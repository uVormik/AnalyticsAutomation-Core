namespace App.Mobile.Android.Services.Stubs;

internal sealed class StubMobileAccessContext :
    global::App.Mobile.Android.Services.Abstractions.IMobileAccessContext
{
    private readonly global::App.Mobile.Android.Options.MobileShellOptions _options;
    private readonly global::App.Mobile.Android.Services.Abstractions.IFeatureFlagReader _featureFlagReader;

    public StubMobileAccessContext(
        global::Microsoft.Extensions.Options.IOptions<global::App.Mobile.Android.Options.MobileShellOptions> options,
        global::App.Mobile.Android.Services.Abstractions.IFeatureFlagReader featureFlagReader)
    {
        _options = options.Value;
        _featureFlagReader = featureFlagReader;
    }

    public global::App.Mobile.Android.State.MobileShellMode CurrentMode => _options.DefaultMode;

    public bool CanAccess(global::App.Mobile.Android.Navigation.MobileViewId viewId)
    {
        return viewId switch
        {
            global::App.Mobile.Android.Navigation.MobileViewId.Reports =>
                _featureFlagReader.IsEnabled(StubFeatureFlagReader.ReportDraftShellFlag),
            global::App.Mobile.Android.Navigation.MobileViewId.Queue => true,
            global::App.Mobile.Android.Navigation.MobileViewId.Profile => true,
            global::App.Mobile.Android.Navigation.MobileViewId.Home => false,
            global::App.Mobile.Android.Navigation.MobileViewId.Upload => false,
            _ => false
        };
    }

    public string GetBannerText()
    {
        if (!_options.ShowShellBanner)
        {
            return string.Empty;
        }

        if (!_featureFlagReader.IsEnabled(StubFeatureFlagReader.ShellBannerFlag))
        {
            return string.Empty;
        }

        return global::App.Mobile.Android.Localization.MobileUiText.GetShellBannerText(CurrentMode);
    }
}