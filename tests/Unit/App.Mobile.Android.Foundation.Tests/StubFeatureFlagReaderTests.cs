namespace App.Mobile.Android.Foundation.Tests;

public sealed class StubFeatureFlagReaderTests
{
    private static readonly global::App.Mobile.Android.Services.Stubs.StubFeatureFlagReader Reader = new();

    [Fact]
    public void ShellBannerFlagIsEnabled()
    {
        Assert.True(Reader.IsEnabled(global::App.Mobile.Android.Services.Stubs.StubFeatureFlagReader.ShellBannerFlag));
    }

    [Fact]
    public void BusinessObjectBindingBlockerCardFlagIsEnabled()
    {
        Assert.True(Reader.IsEnabled(global::App.Mobile.Android.Services.Stubs.StubFeatureFlagReader.BusinessObjectBindingBlockerCardFlag));
    }

    [Fact]
    public void UnknownFlagIsDisabled()
    {
        Assert.False(Reader.IsEnabled("mobile.unknown.flag"));
    }
}