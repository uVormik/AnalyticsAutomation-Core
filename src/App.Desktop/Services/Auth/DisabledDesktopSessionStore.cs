using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class DisabledDesktopSessionStore : IDesktopSessionStore
{
    private const string DisabledReason =
        "Secure desktop token persistence is disabled in S2-50; approved Windows secure storage requires a separate task.";

    public ValueTask<DesktopStoredSession?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult<DesktopStoredSession?>(null);
    }

    public ValueTask<DesktopSessionStoreResult> SaveAsync(
        DesktopStoredSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        // TODO(S2-50 follow-up): replace only after an approved Windows secure token storage task.
        return ValueTask.FromResult(DesktopSessionStoreResult.Disabled(DisabledReason));
    }

    public ValueTask<DesktopSessionStoreResult> ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopSessionStoreResult.Disabled(DisabledReason));
    }
}