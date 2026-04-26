namespace App.Desktop.Boundaries;

public interface IDesktopAuthSessionBoundary
{
    ValueTask<DesktopAuthSessionSnapshot> GetCurrentSessionAsync(CancellationToken cancellationToken);

    ValueTask SignOutAsync(CancellationToken cancellationToken);
}

public sealed record DesktopAuthSessionSnapshot(
    bool IsAuthenticated,
    string? DisplayName);