using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;
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
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
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
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
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
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<FakeDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
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
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<FakeDesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
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
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<FakeDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<FakeDesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
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
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
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
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<FakeDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#else
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#endif
    }

    [Fact]
    public void EnvironmentOptionsEnableFakeUploadFileHashAndBusinessObjectKeyTogetherForVisualSmoke()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadFileEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeUploadHashEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakeBusinessObjectKeyEnabledEnvironmentVariable, "true")
            .Set(DesktopUploadSectionOptions.DevFakePreUploadCheckEnabledEnvironmentVariable, "true");

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

#if DEBUG
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<FakeDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<FakeDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<FakeDesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(services.GetRequiredService<DesktopUploadSectionViewModel>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<FakeDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#else
        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadFileEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeUploadHashEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(services.GetRequiredService<DesktopUploadSectionOptions>().IsDevFakePreUploadCheckEnabled);
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<WpfDesktopVideoFilePicker>(services.GetRequiredService<IDesktopVideoFilePicker>());
        Assert.IsType<DesktopVideoHashService>(services.GetRequiredService<IDesktopVideoHashService>());
        Assert.IsType<DisabledDesktopPreUploadCheckClient>(
            services.GetRequiredService<IDesktopPreUploadCheckClient>());
#endif
    }

    [Fact]
    public void BuildServicesWithConfiguredBaseAddressResolvesHttpAuthClient()
    {
        using var services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress("https://control-plane.local"));

        Assert.IsType<HttpDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
        Assert.IsType<DisabledDesktopSessionStore>(services.GetRequiredService<IDesktopSessionStore>());
        Assert.True(services.GetRequiredService<DesktopAuthOptions>().IsControlPlaneSignInConfigured);
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