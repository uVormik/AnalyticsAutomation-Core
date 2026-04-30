using System.Net.Http;

using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;
using App.Desktop.Services.Placeholders;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Composition;

public static class DesktopCompositionRoot
{
    public static ServiceProvider BuildServices()
    {
        return BuildServices(DesktopAuthOptions.Disabled);
    }

    public static ServiceProvider BuildServicesFromEnvironment()
    {
        return BuildServices(DesktopAuthOptions.FromEnvironment());
    }

    public static ServiceProvider BuildServices(DesktopAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);

        var services = new ServiceCollection();

        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddSingleton(authOptions);
        services.AddSingleton<IDesktopShellLifecycle, PlaceholderDesktopShellLifecycle>();
        services.AddSingleton<IDesktopSessionStore, DisabledDesktopSessionStore>();
        services.AddSingleton<DesktopSessionState>();
        services.AddSingleton<IDesktopSessionState>(
            serviceProvider => serviceProvider.GetRequiredService<DesktopSessionState>());
        services.AddSingleton<IDesktopAuthSessionBoundary>(
            serviceProvider => serviceProvider.GetRequiredService<DesktopSessionState>());
        if (authOptions.IsDevFakeAuthEnabled)
        {
            services.AddSingleton<IDesktopAuthClient, FakeDesktopAuthClient>();
        }
        else if (authOptions.IsControlPlaneSignInConfigured)
        {
            services.AddSingleton(_ => new HttpClient
            {
                BaseAddress = authOptions.ControlPlaneBaseAddress
            });
            services.AddSingleton<IDesktopAuthClient, HttpDesktopAuthClient>();
        }
        else
        {
            services.AddSingleton<IDesktopAuthClient, UnavailableDesktopAuthClient>();
        }

        services.AddSingleton<IDesktopSignInService, DesktopSignInService>();
        services.AddTransient<DesktopSignInViewModel>();
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