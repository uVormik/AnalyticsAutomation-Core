using App.Desktop.Boundaries;
using App.Desktop.Components;
using App.UI.Shared.Pages;

namespace App.Desktop.Services.Placeholders;

public sealed class DesktopBlazorWebViewHostBoundary : IBlazorWebViewHostBoundary
{
    public string SurfaceName => "WPF BlazorWebView host";

    public string HostPage => "wwwroot/index.html";

    public IReadOnlyList<BlazorRootComponentDescriptor> GetRootComponents() =>
    [
        new("#app", typeof(DesktopShell))
    ];

    public IReadOnlyList<SharedUiComponentDescriptor> GetSelectedSharedComponents() =>
    [
        new(nameof(UploadPlaceholder), typeof(UploadPlaceholder))
    ];
}