using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Tests;

public sealed class DesktopCompositionRootTests
{
    [Fact]
    public void BuildServicesWithoutConfiguredBaseAddressKeepsUnavailableAuthClient()
    {
        using var services = DesktopCompositionRoot.BuildServices();

        Assert.IsType<DesktopSessionState>(services.GetRequiredService<IDesktopSessionState>());
        Assert.IsType<DesktopSessionState>(services.GetRequiredService<IDesktopAuthSessionBoundary>());
        Assert.IsType<DisabledDesktopSessionStore>(services.GetRequiredService<IDesktopSessionStore>());
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<DesktopSignInService>(services.GetRequiredService<IDesktopSignInService>());
        Assert.IsType<DesktopUploadSectionViewModel>(services.GetRequiredService<DesktopUploadSectionViewModel>());
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadReceiptEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<DisabledDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
        Assert.IsType<DesktopGroupTreeViewModel>(services.GetRequiredService<DesktopGroupTreeViewModel>());
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<DisabledDesktopUploadReceiptClient>(
            services.GetRequiredService<IDesktopUploadReceiptClient>());
    }

    [Fact]
    public void EnvironmentOptionsKeepFakeAuthDisabledByDefault()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null)
            .Set(DesktopGroupTreeOptions.DevFakeGroupTreeEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadReceiptEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<DisabledDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<DisabledDesktopUploadReceiptClient>(
            services.GetRequiredService<IDesktopUploadReceiptClient>());
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeUploadFileOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<FakeDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeUploadHashOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<FakeDesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeUploadFileAndHashTogetherForVisualSmoke()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<FakeDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<FakeDesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeBusinessObjectKeyOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakePreUploadCheckOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<FakeDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeSiteUploadOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<FakeDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeUploadReceiptOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, null)
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadReceiptEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeUploadReceiptEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<FakeDesktopUploadReceiptClient>(
            services.GetRequiredService<IDesktopUploadReceiptClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadReceiptEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeUploadReceiptEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<DisabledDesktopUploadReceiptClient>(
            services.GetRequiredService<IDesktopUploadReceiptClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeGroupTreeOnlyWhenExplicitlyRequested()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null)
            .Set(DesktopGroupTreeOptions.DevFakeGroupTreeEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.True(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.True(services.GetRequiredService<DesktopGroupTreeViewModel>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<FakeDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
#else
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeViewModel>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<DisabledDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeGroupTreeUploadFileHashBusinessObjectKeyPreUploadCheckSiteUploadAndUploadReceiptTogetherForVisualSmoke()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, "true")
            .Set(DesktopGroupTreeOptions.DevFakeGroupTreeEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeSiteUploadEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadReceiptEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadReceiptEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.True(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<FakeDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<FakeDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
        Assert.IsType<FakeDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<FakeDesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeSiteUploadEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeUploadReceiptEnabled);
        Assert.IsType<FakeDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<FakeDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<FakeDesktopUploadReceiptClient>(
            services.GetRequiredService<IDesktopUploadReceiptClient>());
#else
        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeSiteUploadEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadReceiptEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<DisabledDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<DisabledDesktopUploadReceiptClient>(
            services.GetRequiredService<IDesktopUploadReceiptClient>());
#endif
    }

    [Fact]
    public void BuildServicesWithConfiguredBaseAddressResolvesHttpAuthClient()
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress("https://control-plane.local"));

        Assert.IsType<HttpDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<HttpDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
        Assert.IsType<HttpDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
        Assert.IsType<DisabledDesktopSessionStore>(services.GetRequiredService<IDesktopSessionStore>());
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsControlPlaneSignInConfigured);
        Assert.True(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.Equal(
            new Uri("https://control-plane.local"),
            services.GetRequiredService<HttpClient>().BaseAddress);
    }

    [Fact]
    public void ExplicitFakeAuthFlagResolvesFakeAuthClientOnlyWithoutLiveBaseAddress()
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromEnvironmentValues(
                baseAddress: null,
                devFakeAuthEnabled: "true"));

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.IsType<FakeDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.Null(services.GetService<HttpClient>());
#else
        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
#endif
    }

    [Fact]
    public void ExplicitFakeAuthFlagDoesNotReplaceConfiguredLiveAuthClient()
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromEnvironmentValues(
                "https://control-plane.local",
                devFakeAuthEnabled: "true"));

        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.IsType<HttpDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.Equal(
            new Uri("https://control-plane.local"),
            services.GetRequiredService<HttpClient>().BaseAddress);
    }

    [Fact]
    public void ExplicitFakeGroupTreeFlagDoesNotReplaceConfiguredLiveGroupTreeClient()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, "https://control-plane.local")
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, "true")
            .Set(DesktopGroupTreeOptions.DevFakeGroupTreeEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsControlPlaneSignInConfigured);
        Assert.True(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.True(services.GetRequiredService<DesktopGroupTreeViewModel>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeViewModel>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<HttpDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<HttpDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
    }

    [Fact]
    public void ExplicitFakePreUploadCheckFlagDoesNotReplaceConfiguredLivePreUploadCheckClient()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, "https://control-plane.local")
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsControlPlaneSignInConfigured);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<HttpDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
    }

    [Fact]
    public void ExplicitGroupTreeOptionsCannotOverrideConfiguredLiveBaseAddress()
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress("https://control-plane.local"),
            DesktopUploadSectionOptions.Disabled,
            DesktopGroupTreeOptions.EnabledForDevFakeGroupTree);

        Assert.True(services.GetRequiredService<DesktopGroupTreeOptions>().IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(services.GetRequiredService<DesktopGroupTreeOptions>().IsDevFakeGroupTreeEnabled);
        Assert.IsType<HttpDesktopGroupTreeClient>(services.GetRequiredService<IDesktopGroupTreeClient>());
    }

    [Fact]
    public async Task FakeAuthVisualSmokeCredentialsRemainAvailableForSignedInShellState()
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromEnvironmentValues(
                baseAddress: null,
                devFakeAuthEnabled: "true"));
        var viewModel = services.GetRequiredService<DesktopSignInViewModel>();

        viewModel.Login = FakeDesktopAuthClient.VisualSmokeLogin;
        viewModel.Password = FakeDesktopAuthClient.VisualSmokePassword;

        DesktopSignInResult result = await viewModel.SignInAsync(CancellationToken.None);

#if DEBUG
        Assert.Equal(DesktopSignInStatus.Succeeded, result.Status);
        Assert.True(viewModel.IsSignedIn);
        Assert.Equal("Вход выполнен: Visual Smoke User.", viewModel.SignedInUserContextMessage);
        Assert.Contains(
            DesktopSignedInShellText.NavigationCards,
            card => card.Target == DesktopNavigationCardTarget.UploadSection
                && string.Equals(card.Title, DesktopUploadSectionText.Title, StringComparison.Ordinal));
        Assert.DoesNotContain(FakeDesktopAuthClient.VisualSmokePassword, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("dev-fake-auth-access", result.ToString(), StringComparison.Ordinal);
#else
        Assert.Equal(DesktopSignInStatus.Unavailable, result.Status);
        Assert.False(viewModel.IsSignedIn);
#endif
    }

    [Theory]
    [InlineData("https://control-plane.local")]
    [InlineData("https://control-plane.local/")]
    [InlineData("http://control-plane.local")]
    [InlineData("http://control-plane.local/")]
    [InlineData("http://192.168.1.66/")]
    public void RootBaseAddressOptionsAreConfigured(string baseAddress)
    {
        var options = DesktopAuthOptions.FromControlPlaneBaseAddress(baseAddress);

        Assert.True(options.IsControlPlaneSignInConfigured);
        Assert.NotNull(options.ControlPlaneBaseAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a uri")]
    [InlineData("ftp://control-plane.local")]
    [InlineData("https://operator:secret@control-plane.local")]
    [InlineData("https://control-plane.local?token=secret")]
    [InlineData("https://control-plane.local#secret")]
    [InlineData("https://control-plane.local/prefix")]
    [InlineData("https://control-plane.local/prefix/")]
    public void InvalidBaseAddressOptionsKeepUnavailableAuthClient(string baseAddress)
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress(baseAddress));

        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsControlPlaneSignInConfigured);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = [];

        public EnvironmentVariableScope Set(string name, string? value)
        {
            if (!_originalValues.ContainsKey(name))
            {
                _originalValues.Add(name, Environment.GetEnvironmentVariable(name));
            }

            Environment.SetEnvironmentVariable(name, value);
            return this;
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, string?> entry in _originalValues)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
        }
    }
}