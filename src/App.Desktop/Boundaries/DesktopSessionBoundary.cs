namespace App.Desktop.Boundaries;

public interface IDesktopSessionState
{
    DesktopSessionSnapshot Current { get; }

    ValueTask<DesktopSessionSnapshot> LoadAsync(CancellationToken cancellationToken);

    ValueTask<DesktopSessionSnapshot> SetSignedInAsync(
        DesktopAuthenticatedSession session,
        CancellationToken cancellationToken);

    ValueTask<DesktopSessionSnapshot> SignOutAsync(CancellationToken cancellationToken);
}

public interface IDesktopControlPlaneAccessTokenProvider
{
    ValueTask<DesktopControlPlaneAccessTokenSnapshot> GetCurrentAccessTokenAsync(
        CancellationToken cancellationToken);
}

public interface IDesktopSessionStore
{
    ValueTask<DesktopStoredSession?> LoadAsync(CancellationToken cancellationToken);

    ValueTask<DesktopSessionStoreResult> SaveAsync(
        DesktopStoredSession session,
        CancellationToken cancellationToken);

    ValueTask<DesktopSessionStoreResult> ClearAsync(CancellationToken cancellationToken);
}

public sealed record DesktopSessionSnapshot(
    DesktopSessionStatus Status,
    Guid? UserId,
    string? DisplayName,
    bool HasAccessToken,
    bool HasRefreshToken,
    DateTimeOffset? ExpiresAtUtc)
{
    public static DesktopSessionSnapshot SignedOut { get; } = new(
        DesktopSessionStatus.SignedOut,
        UserId: null,
        DisplayName: null,
        HasAccessToken: false,
        HasRefreshToken: false,
        ExpiresAtUtc: null);

    public bool IsSignedIn => Status == DesktopSessionStatus.SignedIn;

    public static DesktopSessionSnapshot FromSession(DesktopAuthenticatedSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new DesktopSessionSnapshot(
            DesktopSessionStatus.SignedIn,
            session.UserId,
            session.DisplayName,
            HasAccessToken: true,
            HasRefreshToken: !string.IsNullOrWhiteSpace(session.RefreshToken),
            session.ExpiresAtUtc);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopSessionSnapshot)} {{ Status = {Status}, UserId = {UserId}, "
            + $"DisplayName = {DisplayName}, HasAccessToken = {HasAccessToken}, "
            + $"HasRefreshToken = {HasRefreshToken}, ExpiresAtUtc = {ExpiresAtUtc:O} }}";
    }
}

public enum DesktopSessionStatus
{
    SignedOut,
    SignedIn
}

public sealed record DesktopControlPlaneAccessTokenSnapshot(
    bool IsAuthenticated,
    string? AccessToken)
{
    public bool HasAccessToken => IsAuthenticated && !string.IsNullOrWhiteSpace(AccessToken);

    public override string ToString()
    {
        return $"{nameof(DesktopControlPlaneAccessTokenSnapshot)} {{ IsAuthenticated = {IsAuthenticated}, "
            + $"HasAccessToken = {HasAccessToken} }}";
    }
}

public sealed class DesktopAuthenticatedSession
{
    private DesktopAuthenticatedSession(
        Guid sessionId,
        Guid userId,
        Guid? deviceId,
        string? displayName,
        string accessToken,
        string? refreshToken,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        bool isOfflineRestricted)
    {
        SessionId = sessionId;
        UserId = userId;
        DeviceId = deviceId;
        DisplayName = displayName;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IsOfflineRestricted = isOfflineRestricted;
    }

    public Guid SessionId { get; }

    public Guid UserId { get; }

    public Guid? DeviceId { get; }

    public string? DisplayName { get; }

    public string AccessToken { get; }

    public string? RefreshToken { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public bool IsOfflineRestricted { get; }

    public static DesktopAuthenticatedSession Create(
        Guid sessionId,
        Guid userId,
        Guid? deviceId,
        string? displayName,
        string accessToken,
        string? refreshToken,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        bool isOfflineRestricted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return new DesktopAuthenticatedSession(
            sessionId,
            userId,
            deviceId,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            accessToken,
            string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken,
            issuedAtUtc,
            expiresAtUtc,
            isOfflineRestricted);
    }

    public static DesktopAuthenticatedSession FromStoredSession(DesktopStoredSession storedSession)
    {
        ArgumentNullException.ThrowIfNull(storedSession);

        return Create(
            storedSession.SessionId,
            storedSession.UserId,
            storedSession.DeviceId,
            storedSession.DisplayName,
            storedSession.AccessToken,
            storedSession.RefreshToken,
            storedSession.IssuedAtUtc,
            storedSession.ExpiresAtUtc,
            storedSession.IsOfflineRestricted);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopAuthenticatedSession)} {{ SessionId = {SessionId}, UserId = {UserId}, DeviceId = {DeviceId}, DisplayName = {DisplayName}, HasAccessToken = True, HasRefreshToken = {!string.IsNullOrWhiteSpace(RefreshToken)}, ExpiresAtUtc = {ExpiresAtUtc:O}, IsOfflineRestricted = {IsOfflineRestricted} }}";
    }
}

public sealed class DesktopStoredSession
{
    private DesktopStoredSession(
        Guid sessionId,
        Guid userId,
        Guid? deviceId,
        string? displayName,
        string accessToken,
        string? refreshToken,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        bool isOfflineRestricted)
    {
        SessionId = sessionId;
        UserId = userId;
        DeviceId = deviceId;
        DisplayName = displayName;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IsOfflineRestricted = isOfflineRestricted;
    }

    public Guid SessionId { get; }

    public Guid UserId { get; }

    public Guid? DeviceId { get; }

    public string? DisplayName { get; }

    public string AccessToken { get; }

    public string? RefreshToken { get; }

    public DateTimeOffset IssuedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public bool IsOfflineRestricted { get; }

    public static DesktopStoredSession Create(
        Guid sessionId,
        Guid userId,
        Guid? deviceId,
        string? displayName,
        string accessToken,
        string? refreshToken,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        bool isOfflineRestricted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return new DesktopStoredSession(
            sessionId,
            userId,
            deviceId,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            accessToken,
            string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken,
            issuedAtUtc,
            expiresAtUtc,
            isOfflineRestricted);
    }

    public static DesktopStoredSession FromAuthenticatedSession(DesktopAuthenticatedSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return Create(
            session.SessionId,
            session.UserId,
            session.DeviceId,
            session.DisplayName,
            session.AccessToken,
            session.RefreshToken,
            session.IssuedAtUtc,
            session.ExpiresAtUtc,
            session.IsOfflineRestricted);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopStoredSession)} {{ SessionId = {SessionId}, UserId = {UserId}, DeviceId = {DeviceId}, DisplayName = {DisplayName}, HasAccessToken = True, HasRefreshToken = {!string.IsNullOrWhiteSpace(RefreshToken)}, ExpiresAtUtc = {ExpiresAtUtc:O}, IsOfflineRestricted = {IsOfflineRestricted} }}";
    }
}

public sealed record DesktopSessionStoreResult(
    DesktopSessionStoreStatus Status,
    string Reason)
{
    public static DesktopSessionStoreResult Saved(string reason)
    {
        return new DesktopSessionStoreResult(DesktopSessionStoreStatus.Saved, reason);
    }

    public static DesktopSessionStoreResult Cleared(string reason)
    {
        return new DesktopSessionStoreResult(DesktopSessionStoreStatus.Cleared, reason);
    }

    public static DesktopSessionStoreResult Disabled(string reason)
    {
        return new DesktopSessionStoreResult(DesktopSessionStoreStatus.Disabled, reason);
    }
}

public enum DesktopSessionStoreStatus
{
    Saved,
    Cleared,
    Disabled
}