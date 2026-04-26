using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderDesktopShellLifecycle : IDesktopShellLifecycle
{
    public DesktopShellLifecycleState State { get; private set; } = DesktopShellLifecycleState.Created;

    public Task OnStartingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        State = DesktopShellLifecycleState.Started;
        return Task.CompletedTask;
    }

    public Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        State = DesktopShellLifecycleState.Stopped;
        return Task.CompletedTask;
    }
}