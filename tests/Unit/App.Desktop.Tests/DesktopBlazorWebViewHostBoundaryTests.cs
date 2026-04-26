using App.Desktop.Components;
using App.Desktop.Services.Placeholders;

namespace App.Desktop.Tests;

public sealed class DesktopBlazorWebViewHostBoundaryTests
{
    [Fact]
    public void GetRootComponentsUsesLocalDesktopShellAndNotAppWeb()
    {
        var boundary = new DesktopBlazorWebViewHostBoundary();

        var root = Assert.Single(boundary.GetRootComponents());

        Assert.Equal("#app", root.Selector);
        Assert.Equal(typeof(DesktopShell), root.ComponentType);
        Assert.NotEqual("App.Web", root.ComponentType.Assembly.GetName().Name);
    }

    [Fact]
    public void GetSelectedSharedComponentsUsesAppUiSharedPlaceholder()
    {
        var boundary = new DesktopBlazorWebViewHostBoundary();

        var sharedComponent = Assert.Single(boundary.GetSelectedSharedComponents());

        Assert.Equal("UploadPlaceholder", sharedComponent.Name);
        Assert.Equal("App.UI.Shared", sharedComponent.ComponentType.Assembly.GetName().Name);
    }
}