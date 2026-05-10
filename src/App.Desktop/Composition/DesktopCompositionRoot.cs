using System.Net.Http;

using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
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
        DesktopAuthOptions authOptions = DesktopAuthOptions.FromEnvironment();

        return BuildServices(
            authOptions,
            DesktopUploadSectionOptions.FromEnvironment(),
            DesktopGroupTreeOptions.FromAuthOptions(authOptions),
            DesktopDirectSiteProviderOptions.FromEnvironment());
    }

    public static ServiceProvider BuildServices(DesktopAuthOptions authOptions)
    {
        return BuildServices(
            authOptions,
            DesktopUploadSectionOptions.Disabled,
            DesktopGroupTreeOptions.FromAuthOptions(authOptions, devFakeGroupTreeEnabled: null),
            DesktopDirectSiteProviderOptions.Disabled);
    }

    public static ServiceProvider BuildServices(
        DesktopAuthOptions authOptions,
        DesktopUploadSectionOptions uploadSectionOptions)
    {
        return BuildServices(
            authOptions,
            uploadSectionOptions,
            DesktopGroupTreeOptions.FromAuthOptions(authOptions, devFakeGroupTreeEnabled: null),
            DesktopDirectSiteProviderOptions.Disabled);
    }

    public static ServiceProvider BuildServices(
        DesktopAuthOptions authOptions,
        DesktopUploadSectionOptions uploadSectionOptions,
        DesktopGroupTreeOptions groupTreeOptions)
    {
        return BuildServices(
            authOptions,
            uploadSectionOptions,
            groupTreeOptions,
            DesktopDirectSiteProviderOptions.Disabled);
    }

    public static ServiceProvider BuildServices(
        DesktopAuthOptions authOptions,
        DesktopUploadSectionOptions uploadSectionOptions,
        DesktopGroupTreeOptions groupTreeOptions,
        DesktopDirectSiteProviderOptions directSiteProviderOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);
        ArgumentNullException.ThrowIfNull(uploadSectionOptions);
        ArgumentNullException.ThrowIfNull(groupTreeOptions);
        ArgumentNullException.ThrowIfNull(directSiteProviderOptions);

        DesktopGroupTreeOptions effectiveGroupTreeOptions = authOptions.IsControlPlaneSignInConfigured
            ? DesktopGroupTreeOptions.EnabledForLiveControlPlane
            : groupTreeOptions;
        DesktopUploadSectionOptions effectiveUploadSectionOptions = authOptions.IsControlPlaneSignInConfigured
            ? uploadSectionOptions
                .WithLiveControlPlanePreUploadCheck()
                .WithLiveControlPlaneUploadReceipt()
                .WithoutDevFakeSiteUpload()
            : uploadSectionOptions;

        var services = new ServiceCollection();

        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddSingleton(authOptions);
        services.AddSingleton(effectiveUploadSectionOptions);
        services.AddSingleton(effectiveGroupTreeOptions);
        services.AddSingleton(directSiteProviderOptions);
        services.AddSingleton<IDesktopShellLifecycle, PlaceholderDesktopShellLifecycle>();
        services.AddSingleton<IDesktopSessionStore, DisabledDesktopSessionStore>();
        services.AddSingleton<DesktopSessionState>();
        services.AddSingleton<DesktopGroupSelectionState>();
        services.AddSingleton<IDesktopSessionState>(
            serviceProvider => serviceProvider.GetRequiredService<DesktopSessionState>());
        services.AddSingleton<IDesktopAuthSessionBoundary>(
            serviceProvider => serviceProvider.GetRequiredService<DesktopSessionState>());
        services.AddSingleton<IDesktopControlPlaneAccessTokenProvider>(
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
        services.AddTransient<DesktopGroupTreeViewModel>();
        services.AddTransient<DesktopUploadSectionViewModel>();
        services.AddSingleton<ISecureSessionStorage, PlaceholderSecureSessionStorage>();
        if (effectiveGroupTreeOptions.IsLiveControlPlaneGroupTreeEnabled
            && authOptions.IsControlPlaneSignInConfigured)
        {
            services.AddSingleton<IDesktopGroupTreeClient, HttpDesktopGroupTreeClient>();
        }
        else if (effectiveGroupTreeOptions.IsDevFakeGroupTreeEnabled)
        {
            services.AddSingleton<IDesktopGroupTreeClient, FakeDesktopGroupTreeClient>();
        }
        else
        {
            services.AddSingleton<IDesktopGroupTreeClient, DisabledDesktopGroupTreeClient>();
        }

        if (effectiveUploadSectionOptions.IsDevFakeUploadFileEnabled)
        {
            services.AddSingleton<IDesktopVideoFilePicker, FakeDesktopVideoFilePicker>();
        }
        else
        {
            services.AddSingleton<IDesktopVideoFilePicker, WpfDesktopVideoFilePicker>();
        }

        if (effectiveUploadSectionOptions.IsDevFakeUploadHashEnabled)
        {
            services.AddSingleton<IDesktopVideoHashService, FakeDesktopVideoHashService>();
        }
        else
        {
            services.AddSingleton<IDesktopVideoHashService, DesktopVideoHashService>();
        }

        if (effectiveUploadSectionOptions.IsLiveControlPlanePreUploadCheckEnabled
            && authOptions.IsControlPlaneSignInConfigured)
        {
            services.AddSingleton<IDesktopPreUploadCheckClient, HttpDesktopPreUploadCheckClient>();
        }
        else if (effectiveUploadSectionOptions.IsDevFakePreUploadCheckEnabled)
        {
            services.AddSingleton<IDesktopPreUploadCheckClient, FakeDesktopPreUploadCheckClient>();
        }
        else
        {
            services.AddSingleton<IDesktopPreUploadCheckClient, DisabledDesktopPreUploadCheckClient>();
        }

        if (effectiveUploadSectionOptions.IsDevFakeSiteUploadEnabled)
        {
            services.AddSingleton<IDesktopDirectSiteUploadClient, FakeDesktopDirectSiteUploadClient>();
        }
        else
        {
            services.AddSingleton<IDesktopDirectSiteUploadClient, DisabledDesktopDirectSiteUploadClient>();
        }

        if (effectiveUploadSectionOptions.IsLiveControlPlaneUploadReceiptEnabled
            && authOptions.IsControlPlaneSignInConfigured)
        {
            services.AddSingleton<IDesktopUploadReceiptClient, HttpDesktopUploadReceiptClient>();
        }
        else if (effectiveUploadSectionOptions.IsDevFakeUploadReceiptEnabled)
        {
            services.AddSingleton<IDesktopUploadReceiptClient, FakeDesktopUploadReceiptClient>();
        }
        else
        {
            services.AddSingleton<IDesktopUploadReceiptClient, DisabledDesktopUploadReceiptClient>();
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