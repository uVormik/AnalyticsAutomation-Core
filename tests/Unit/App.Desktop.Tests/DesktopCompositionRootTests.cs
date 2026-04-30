using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;

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
    }

    [Fact]
    public void EnvironmentOptionsKeepFakeAuthDisabledByDefault()
    {
        using var environment = new EnvironmentVariableScope()
            .Set(DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable, null)
            .Set(DesktopAuthOptions.DevFakeAuthEnabledEnvironmentVariable, null);

        using var services = DesktopCompositionRoot.BuildServicesFromEnvironment();

        Assert.False(services.GetRequiredService<DesktopAuthOptions>().IsDevFakeAuthEnabled);
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
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