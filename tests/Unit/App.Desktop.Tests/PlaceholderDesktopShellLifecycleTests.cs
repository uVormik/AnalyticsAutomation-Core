using App.Desktop.Boundaries;
using App.Desktop.Services.Placeholders;

namespace App.Desktop.Tests;

public sealed class PlaceholderDesktopShellLifecycleTests
{
    [Fact]
    public async Task OnStartingAndStoppingAsyncTracksLifecycleState()
    {
        var lifecycle = new PlaceholderDesktopShellLifecycle();

        Assert.Equal(DesktopShellLifecycleState.Created, lifecycle.State);

        await lifecycle.OnStartingAsync(CancellationToken.None);
        Assert.Equal(DesktopShellLifecycleState.Started, lifecycle.State);

        await lifecycle.OnStoppingAsync(CancellationToken.None);
        Assert.Equal(DesktopShellLifecycleState.Stopped, lifecycle.State);
    }
}