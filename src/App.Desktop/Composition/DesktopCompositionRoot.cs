using System.Net.Http;

using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;
using App.Desktop.Services.Placeholders;
using App.Desktop.Services.Upload;

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
        return BuildServices(
            DesktopAuthOptions.FromEnvironment(),
            DesktopUploadSectionOptions.FromEnvironment());
    }

    public static ServiceProvider BuildServices(DesktopAuthOptions authOptions)
    {
        return BuildServices(authOptions, DesktopUploadSectionOptions.Disabled);
    }

    public static ServiceProvider BuildServices(
        DesktopAuthOptions authOptions,
        DesktopUploadSectionOptions uploadSectionOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);
        ArgumentNullException.ThrowIfNull(uploadSectionOptions);

        var services = new ServiceCollection();

        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddSingleton(authOptions);
        services.AddSingleton(uploadSectionOptions);
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
        services.AddTransient<DesktopUploadSectionViewModel>();
        services.AddSingleton<ISecureSessionStorage, PlaceholderSecureSessionStorage>();
        if (uploadSectionOptions.IsDevFakeUploadFileEnabled)
        {
            services.AddSingleton<IDesktopVideoFilePicker, FakeDesktopVideoFilePicker>();
        }
        else
        {
            services.AddSingleton<IDesktopVideoFilePicker, WpfDesktopVideoFilePicker>();
        }

        if (uploadSectionOptions.IsDevFakeUploadHashEnabled)
        {
            services.AddSingleton<IDesktopVideoHashService, FakeDesktopVideoHashService>();
        }
        else
        {
            services.AddSingleton<IDesktopVideoHashService, DesktopVideoHashService>();
        }

        if (uploadSectionOptions.IsDevFakePreUploadCheckEnabled)
        {
            services.AddSingleton<IDesktopPreUploadCheckClient, FakeDesktopPreUploadCheckClient>();
        }
        else
        {
            services.AddSingleton<IDesktopPreUploadCheckClient, DisabledDesktopPreUploadCheckClient>();
        }

        if (uploadSectionOptions.IsDevFakeSiteUploadEnabled)
        {
            services.AddSingleton<IDesktopDirectSiteUploadClient, FakeDesktopDirectSiteUploadClient>();
        }
        else
        {
            services.AddSingleton<IDesktopDirectSiteUploadClient, DisabledDesktopDirectSiteUploadClient>();
        }

        services.AddSingleton<IDesktopFilePicker, PlaceholderDesktopFilePicker>();
        services.AddSingleton<ILocalFileMetadataService, PlaceholderLocalFileMetadataService>();
        services.AddSingleton<IControlPlaneApiClient, PlaceholderControlPlaneApiClient>();
        services.AddSingleton<IDirectSiteUploadAdapter, PlaceholderDirectSiteUploadAdapter>();
        services.AddSingleton<IDesktopUploadOrchestrator, PlaceholderDesktopUploadOrchestrator>();
        services.AddSingleton<IBlazorWebViewHostBoundary, DesktopBlazorWebViewHostBoundary>();

        return services.BuildServiceProvider();
    }
}