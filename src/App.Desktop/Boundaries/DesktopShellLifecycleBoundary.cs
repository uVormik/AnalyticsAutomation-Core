namespace App.Desktop.Boundaries;

public interface IDesktopShellLifecycle
{
    DesktopShellLifecycleState State { get; }

    Task OnStartingAsync(CancellationToken cancellationToken);

    Task OnStoppingAsync(CancellationToken cancellationToken);
}

public enum DesktopShellLifecycleState
{
    Created,
    Started,
    Stopped
}