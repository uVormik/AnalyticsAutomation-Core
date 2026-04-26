namespace App.Mobile.Android.Foundation.Tests;

public sealed class StubMobileAccessContextTests
{
    [Fact]
    public void DefaultModeIsDevelopment()
    {
        var accessContext = CreateAccessContext();

        Assert.Equal(global::App.Mobile.Android.State.MobileShellMode.Development, accessContext.CurrentMode);
    }

    [Fact]
    public void CanAccessReturnsTrueForReportFirstPrimaryTabs()
    {
        var accessContext = CreateAccessContext();

        Assert.True(accessContext.CanAccess(global::App.Mobile.Android.Navigation.MobileViewId.Reports));
        Assert.True(accessContext.CanAccess(global::App.Mobile.Android.Navigation.MobileViewId.Queue));
        Assert.True(accessContext.CanAccess(global::App.Mobile.Android.Navigation.MobileViewId.Profile));
    }

    [Fact]
    public void CanAccessKeepsHomeAndUploadOutOfPrimaryNavigation()
    {
        var accessContext = CreateAccessContext();

        Assert.False(accessContext.CanAccess(global::App.Mobile.Android.Navigation.MobileViewId.Home));
        Assert.False(accessContext.CanAccess(global::App.Mobile.Android.Navigation.MobileViewId.Upload));
    }

    [Fact]
    public void GetBannerTextReturnsNonEmptyRussianStubText()
    {
        var accessContext = CreateAccessContext();

        var bannerText = accessContext.GetBannerText();

        Assert.False(string.IsNullOrWhiteSpace(bannerText));
        Assert.Contains("локаль", bannerText, global::System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("заглуш", bannerText, global::System.StringComparison.OrdinalIgnoreCase);
    }

    private static global::App.Mobile.Android.Services.Stubs.StubMobileAccessContext CreateAccessContext()
    {
        return new global::App.Mobile.Android.Services.Stubs.StubMobileAccessContext(
            global::Microsoft.Extensions.Options.Options.Create(
                new global::App.Mobile.Android.Options.MobileShellOptions()),
            new global::App.Mobile.Android.Services.Stubs.StubFeatureFlagReader());
    }
}