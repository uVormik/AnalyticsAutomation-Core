namespace App.Desktop.Boundaries;

public interface IDesktopAuthClient
{
    ValueTask<DesktopAuthResult> SignInAsync(
        DesktopSignInRequest request,
        CancellationToken cancellationToken);
}

public sealed class DesktopSignInRequest
{
    public DesktopSignInRequest(
        string login,
        string password,
        Guid? deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        Login = login.Trim();
        Password = password;
        DeviceId = deviceId;
    }

    public string Login { get; }

    public string Password { get; }

    public Guid? DeviceId { get; }

    public override string ToString()
    {
        return $"{nameof(DesktopSignInRequest)} {{ HasLogin = True, HasPassword = True, DeviceId = {DeviceId} }}";
    }
}

public sealed class DesktopAuthResult
{
    private DesktopAuthResult(
        DesktopAuthStatus status,
        DesktopAuthenticatedSession? session,
        DesktopAuthError? error)
    {
        Status = status;
        Session = session;
        Error = error;
    }

    public DesktopAuthStatus Status { get; }

    public DesktopAuthenticatedSession? Session { get; }

    public DesktopAuthError? Error { get; }

    public bool IsSuccess => Status == DesktopAuthStatus.Succeeded;

    public static DesktopAuthResult Succeeded(DesktopAuthenticatedSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new DesktopAuthResult(DesktopAuthStatus.Succeeded, session, error: null);
    }

    public static DesktopAuthResult Rejected(string code, string message)
    {
        return new DesktopAuthResult(
            DesktopAuthStatus.Rejected,
            session: null,
            new DesktopAuthError(code, message));
    }

    public static DesktopAuthResult Failed(string code, string message)
    {
        return new DesktopAuthResult(
            DesktopAuthStatus.Failed,
            session: null,
            new DesktopAuthError(code, message));
    }

    public static DesktopAuthResult Unavailable(string code, string message)
    {
        return new DesktopAuthResult(
            DesktopAuthStatus.Unavailable,
            session: null,
            new DesktopAuthError(code, message));
    }

    public override string ToString()
    {
        return $"{nameof(DesktopAuthResult)} {{ Status = {Status}, HasSession = {Session is not null}, Error = {Error} }}";
    }
}

public sealed record DesktopAuthError(
    string Code,
    string Message);

public enum DesktopAuthStatus
{
    Succeeded,
    Rejected,
    Failed,
    Unavailable
}