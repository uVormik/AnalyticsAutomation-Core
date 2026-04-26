using App.Desktop.Boundaries;
using App.Desktop.Services.Placeholders;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Composition;

public static class DesktopCompositionRoot
{
    public static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddSingleton<IDesktopShellLifecycle, PlaceholderDesktopShellLifecycle>();
        services.AddSingleton<IDesktopAuthSessionBoundary, PlaceholderDesktopAuthSessionBoundary>();
        services.AddSingleton<ISecureSessionStorage, PlaceholderSecureSessionStorage>();
        services.AddSingleton<IDesktopFilePicker, PlaceholderDesktopFilePicker>();
        services.AddSingleton<ILocalFileMetadataService, PlaceholderLocalFileMetadataService>();
        services.AddSingleton<IControlPlaneApiClient, PlaceholderControlPlaneApiClient>();
        services.AddSingleton<IDirectSiteUploadAdapter, PlaceholderDirectSiteUploadAdapter>();
        services.AddSingleton<IDesktopUploadOrchestrator, PlaceholderDesktopUploadOrchestrator>();
        services.AddSingleton<IBlazorWebViewHostBoundary, DesktopBlazorWebViewHostBoundary>();

        return services.BuildServiceProvider();
    }
}