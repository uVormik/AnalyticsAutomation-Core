using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;

namespace App.Desktop.Tests;

public sealed class DesktopSessionStateTests
{
    [Fact]
    public async Task SessionStateStartsSignedOut()
    {
        var state = new DesktopSessionState(new RecordingDesktopSessionStore());

        Assert.Equal(DesktopSessionStatus.SignedOut, state.Current.Status);
        Assert.False(state.Current.IsSignedIn);
        Assert.False(state.Current.HasAccessToken);
        Assert.False(state.Current.HasRefreshToken);

        var snapshot = await state.LoadAsync(CancellationToken.None);

        Assert.Equal(DesktopSessionStatus.SignedOut, snapshot.Status);
        Assert.False(snapshot.IsSignedIn);
        Assert.Null(snapshot.UserId);
        Assert.Null(snapshot.DisplayName);
    }

    [Fact]
    public async Task SetSignedInSessionUpdatesSanitizedSnapshotAndStoresThroughBoundary()
    {
        var store = new RecordingDesktopSessionStore();
        var state = new DesktopSessionState(store);
        var session = CreateAuthenticatedSession();

        var snapshot = await state.SetSignedInAsync(session, CancellationToken.None);

        Assert.Equal(DesktopSessionStatus.SignedIn, snapshot.Status);
        Assert.True(snapshot.IsSignedIn);
        Assert.Equal(session.UserId, snapshot.UserId);
        Assert.Equal(session.DisplayName, snapshot.DisplayName);
        Assert.True(snapshot.HasAccessToken);
        Assert.True(snapshot.HasRefreshToken);
        Assert.Equal(session.ExpiresAtUtc, snapshot.ExpiresAtUtc);
        Assert.NotNull(store.SavedSession);
        Assert.Equal(session.AccessToken, store.SavedSession.AccessToken);
    }

    [Fact]
    public async Task SetSignedInSessionDoesNotMutateStateWhenStoreSaveThrows()
    {
        var store = new ThrowingDesktopSessionStore(new InvalidOperationException("store save failed"));
        var state = new DesktopSessionState(store);
        var session = CreateAuthenticatedSession();

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await state.SetSignedInAsync(session, CancellationToken.None));

        var boundarySnapshot = await ((IDesktopAuthSessionBoundary)state).GetCurrentSessionAsync(
            CancellationToken.None);

        Assert.Equal(DesktopSessionStatus.SignedOut, state.Current.Status);
        Assert.False(state.Current.IsSignedIn);
        Assert.False(state.Current.HasAccessToken);
        Assert.False(state.Current.HasRefreshToken);
        Assert.False(boundarySnapshot.IsAuthenticated);
        Assert.Null(boundarySnapshot.DisplayName);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task SignOutClearsCurrentSessionAndStoreBoundary()
    {
        var store = new RecordingDesktopSessionStore();
        var state = new DesktopSessionState(store);

        await state.SetSignedInAsync(CreateAuthenticatedSession(), CancellationToken.None);
        var snapshot = await state.SignOutAsync(CancellationToken.None);

        Assert.Equal(DesktopSessionStatus.SignedOut, snapshot.Status);
        Assert.False(snapshot.IsSignedIn);
        Assert.False(snapshot.HasAccessToken);
        Assert.False(snapshot.HasRefreshToken);
        Assert.Null(snapshot.UserId);
        Assert.Equal(1, store.ClearCount);
        Assert.Same(snapshot, state.Current);
    }

    [Fact]
    public async Task LegacyAuthSessionBoundaryUsesSanitizedSessionState()
    {
        var state = new DesktopSessionState(new RecordingDesktopSessionStore());
        var boundary = (IDesktopAuthSessionBoundary)state;

        await state.SetSignedInAsync(CreateAuthenticatedSession(), CancellationToken.None);

        var snapshot = await boundary.GetCurrentSessionAsync(CancellationToken.None);

        Assert.True(snapshot.IsAuthenticated);
        Assert.Equal("Desktop Operator", snapshot.DisplayName);
    }

    [Fact]
    public async Task SessionDiagnosticsDoNotIncludeTokenValues()
    {
        var store = new RecordingDesktopSessionStore();
        var state = new DesktopSessionState(store);
        var session = CreateAuthenticatedSession();

        var snapshot = await state.SetSignedInAsync(session, CancellationToken.None);

        Assert.DoesNotContain(session.AccessToken, snapshot.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(session.RefreshToken!, snapshot.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(session.AccessToken, session.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(session.RefreshToken!, session.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(session.AccessToken, store.SavedSession!.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(session.RefreshToken!, store.SavedSession.ToString(), StringComparison.Ordinal);
    }

    private static DesktopAuthenticatedSession CreateAuthenticatedSession()
    {
        return DesktopAuthenticatedSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Desktop Operator",
            CreateSensitiveValue("access"),
            CreateSensitiveValue("refresh"),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15),
            isOfflineRestricted: false);
    }

    private static string CreateSensitiveValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private sealed class RecordingDesktopSessionStore : IDesktopSessionStore
    {
        public DesktopStoredSession? SavedSession { get; private set; }

        public int ClearCount { get; private set; }

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

            SavedSession = session;
            return ValueTask.FromResult(DesktopSessionStoreResult.Saved("recorded by test helper"));
        }

        public ValueTask<DesktopSessionStoreResult> ClearAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ClearCount++;
            SavedSession = null;
            return ValueTask.FromResult(DesktopSessionStoreResult.Cleared("cleared by test helper"));
        }
    }

    private sealed class ThrowingDesktopSessionStore(Exception exception) : IDesktopSessionStore
    {
        public int SaveCount { get; private set; }

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

            SaveCount++;
            throw exception;
        }

        public ValueTask<DesktopSessionStoreResult> ClearAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(DesktopSessionStoreResult.Cleared("cleared by test helper"));
        }
    }
}