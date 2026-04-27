using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class DesktopSessionState(IDesktopSessionStore sessionStore) :
    IDesktopSessionState,
    IDesktopAuthSessionBoundary
{
    private DesktopSessionSnapshot _current = DesktopSessionSnapshot.SignedOut;
    private DesktopAuthenticatedSession? _session;

    public DesktopSessionSnapshot Current => _current;

    public async ValueTask<DesktopSessionSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storedSession = await sessionStore.LoadAsync(cancellationToken);
        if (storedSession is null)
        {
            _session = null;
            _current = DesktopSessionSnapshot.SignedOut;
            return _current;
        }

        _session = DesktopAuthenticatedSession.FromStoredSession(storedSession);
        _current = DesktopSessionSnapshot.FromSession(_session);
        return _current;
    }

    public async ValueTask<DesktopSessionSnapshot> SetSignedInAsync(
        DesktopAuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        await sessionStore.SaveAsync(
            DesktopStoredSession.FromAuthenticatedSession(session),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        DesktopSessionSnapshot snapshot = DesktopSessionSnapshot.FromSession(session);

        _session = session;
        _current = snapshot;

        return snapshot;
    }

    public async ValueTask<DesktopSessionSnapshot> SignOutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _session = null;
        _current = DesktopSessionSnapshot.SignedOut;

        await sessionStore.ClearAsync(cancellationToken);

        return _current;
    }

    ValueTask<DesktopAuthSessionSnapshot> IDesktopAuthSessionBoundary.GetCurrentSessionAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new DesktopAuthSessionSnapshot(
            _current.IsSignedIn,
            _current.DisplayName));
    }

    async ValueTask IDesktopAuthSessionBoundary.SignOutAsync(CancellationToken cancellationToken)
    {
        await SignOutAsync(cancellationToken);
    }
}