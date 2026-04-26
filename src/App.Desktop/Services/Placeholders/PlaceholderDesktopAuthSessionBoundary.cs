using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderDesktopAuthSessionBoundary : IDesktopAuthSessionBoundary
{
    public ValueTask<DesktopAuthSessionSnapshot> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new DesktopAuthSessionSnapshot(
            IsAuthenticated: false,
            DisplayName: null));
    }

    public ValueTask SignOutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}