namespace App.Mobile.Android.Foundation.Tests;

public sealed class MobileViewRegistryTests
{
    private static readonly global::App.Mobile.Android.Navigation.MobileViewId[] ExpectedViewIds =
    [
        global::App.Mobile.Android.Navigation.MobileViewId.Reports,
        global::App.Mobile.Android.Navigation.MobileViewId.Queue,
        global::App.Mobile.Android.Navigation.MobileViewId.Profile,
        global::App.Mobile.Android.Navigation.MobileViewId.Home,
        global::App.Mobile.Android.Navigation.MobileViewId.Upload
    ];

    private static readonly string[] ExpectedRoutes = ["/reports", "/queue", "/profile", "/", "/upload"];

    private static readonly string[] ExpectedPrimaryRoutes = ["/reports", "/queue", "/profile"];

    [Fact]
    public void RegistryIncludesReportFirstPrimaryRoutesAndServiceRoutes()
    {
        var registry = new global::App.Mobile.Android.Navigation.MobileViewRegistry();

        var viewIds = registry.MenuEntries.Select(entry => entry.ViewId).ToArray();

        Assert.Equal(ExpectedViewIds, viewIds);
    }

    [Fact]
    public void RegistryIncludesUploadRouteWithoutMakingItPrimary()
    {
        var registry = new global::App.Mobile.Android.Navigation.MobileViewRegistry();

        var routes = registry.MenuEntries.Select(entry => entry.Route).ToArray();

        Assert.Equal(ExpectedRoutes, routes);
    }

    [Fact]
    public void GetVisibleMenuEntriesReturnsReportFirstPrimaryTabsOnlyForDefaultStubAccessContext()
    {
        var registry = new global::App.Mobile.Android.Navigation.MobileViewRegistry();
        var accessContext = CreateAccessContext();

        var visibleEntries = registry.GetVisibleMenuEntries(accessContext).ToArray();

        Assert.Equal(3, visibleEntries.Length);
        Assert.Equal(ExpectedPrimaryRoutes, visibleEntries.Select(entry => entry.Route).ToArray());
        Assert.DoesNotContain(visibleEntries, entry => entry.ViewId == global::App.Mobile.Android.Navigation.MobileViewId.Upload);
    }

    private static global::App.Mobile.Android.Services.Stubs.StubMobileAccessContext CreateAccessContext()
    {
        return new global::App.Mobile.Android.Services.Stubs.StubMobileAccessContext(
            global::Microsoft.Extensions.Options.Options.Create(
                new global::App.Mobile.Android.Options.MobileShellOptions()),
            new global::App.Mobile.Android.Services.Stubs.StubFeatureFlagReader());
    }
}