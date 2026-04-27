using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Tests;

public sealed class DesktopCompositionRootTests
{
    [Fact]
    public void BuildServicesResolvesDesktopAuthAndSessionBoundaries()
    {
        using var services = DesktopCompositionRoot.BuildServices();

        Assert.IsType<DesktopSessionState>(services.GetRequiredService<IDesktopSessionState>());
        Assert.IsType<DesktopSessionState>(services.GetRequiredService<IDesktopAuthSessionBoundary>());
        Assert.IsType<DisabledDesktopSessionStore>(services.GetRequiredService<IDesktopSessionStore>());
        Assert.IsType<UnavailableDesktopAuthClient>(services.GetRequiredService<IDesktopAuthClient>());
    }
}