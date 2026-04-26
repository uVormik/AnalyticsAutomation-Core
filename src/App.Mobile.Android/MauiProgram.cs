using Microsoft.Extensions.Logging;

namespace App.Mobile.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<
            global::Microsoft.Extensions.Options.IValidateOptions<global::App.Mobile.Android.Options.MobileShellOptions>,
            global::App.Mobile.Android.Options.MobileShellOptionsValidator>();
        builder.Services
            .AddOptions<global::App.Mobile.Android.Options.MobileShellOptions>()
            .Configure(options =>
            {
                options.DefaultMode = global::App.Mobile.Android.State.MobileShellMode.Development;
                options.ShowShellBanner = true;
            })
            .ValidateOnStart();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IFeatureFlagReader,
            global::App.Mobile.Android.Services.Stubs.StubFeatureFlagReader>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileAccessContext,
            global::App.Mobile.Android.Services.Stubs.StubMobileAccessContext>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileMediaService,
            global::App.Mobile.Android.Services.Android.AndroidNativeMediaService>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileSelectedMediaSnapshotStore>(
            _ => new global::App.Mobile.Android.Services.Local.FileMobileSelectedMediaSnapshotStore(
                global::Microsoft.Maui.Storage.FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileSelectedMediaStore,
            global::App.Mobile.Android.Services.Local.InMemoryMobileSelectedMediaStore>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileLookupCatalogProvider,
            global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileReportDraftStore,
            global::App.Mobile.Android.Services.Local.InMemoryMobileReportDraftStore>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.ILocalDuplicatePrecheckService,
            global::App.Mobile.Android.Services.Local.LocalOutboxDuplicatePrecheckService>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.ILocalMediaDraftRepairService,
            global::App.Mobile.Android.Services.Local.LocalCurrentSelectionDraftRepairService>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileBusinessObjectBindingService,
            global::App.Mobile.Android.Services.Local.UnresolvedMobileBusinessObjectBindingService>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobilePreUploadEligibilityGate,
            global::App.Mobile.Android.Services.Local.LocalMobilePreUploadEligibilityGate>();
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileOutboxSnapshotStore>(
            _ => new global::App.Mobile.Android.Services.Local.FileMobileOutboxSnapshotStore(
                global::Microsoft.Maui.Storage.FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<
            global::App.Mobile.Android.Services.Abstractions.IMobileOutboxService,
            global::App.Mobile.Android.Services.Stubs.StubMobileOutboxService>();
        builder.Services.AddSingleton<global::App.Mobile.Android.Navigation.MobileViewRegistry>();
        builder.Services.AddSingleton<global::App.Mobile.Android.Navigation.MobileNavigationState>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}