namespace App.Desktop.Boundaries;

public interface IDesktopSignInService
{
    ValueTask<DesktopSignInResult> SignInAsync(
        string login,
        string password,
        Guid? deviceId,
        CancellationToken cancellationToken);
}

public sealed class DesktopSignInResult
{
    private DesktopSignInResult(
        DesktopSignInStatus status,
        DesktopSessionSnapshot? session,
        DesktopSignInError? error,
        string message)
    {
        Status = status;
        Session = session;
        Error = error;
        Message = message;
    }

    public DesktopSignInStatus Status { get; }

    public DesktopSessionSnapshot? Session { get; }

    public DesktopSignInError? Error { get; }

    public string Message { get; }

    public bool IsSuccess => Status == DesktopSignInStatus.Succeeded;

    public static DesktopSignInResult NotStarted { get; } = new(
        DesktopSignInStatus.NotStarted,
        session: null,
        error: null,
        "Sign-in has not been attempted.");

    public static DesktopSignInResult Succeeded(DesktopSessionSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new DesktopSignInResult(
            DesktopSignInStatus.Succeeded,
            session,
            error: null,
            "Signed in.");
    }

    public static DesktopSignInResult Rejected(string? code)
    {
        return WithError(
            DesktopSignInStatus.Rejected,
            code,
            fallbackCode: "sign_in_rejected",
            message: "Sign-in was rejected.");
    }

    public static DesktopSignInResult Failed(string? code)
    {
        return WithError(
            DesktopSignInStatus.Failed,
            code,
            fallbackCode: "sign_in_failed",
            message: "Sign-in failed.");
    }

    public static DesktopSignInResult Unavailable(string? code)
    {
        return WithError(
            DesktopSignInStatus.Unavailable,
            code,
            fallbackCode: "sign_in_unavailable",
            message: "Desktop sign-in is unavailable.");
    }

    public override string ToString()
    {
        return $"{nameof(DesktopSignInResult)} {{ Status = {Status}, HasSession = {Session is not null}, "
            + $"Error = {Error}, Message = {Message} }}";
    }

    private static DesktopSignInResult WithError(
        DesktopSignInStatus status,
        string? code,
        string fallbackCode,
        string message)
    {
        return new DesktopSignInResult(
            status,
            session: null,
            new DesktopSignInError(NormalizeCode(code, fallbackCode), message),
            message);
    }

    private static string NormalizeCode(string? code, string fallbackCode)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return fallbackCode;
        }

        string trimmed = code.Trim();
        if (trimmed.Length > 64)
        {
            return fallbackCode;
        }

        foreach (char value in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(value) && value is not '_' and not '-' and not '.')
            {
                return fallbackCode;
            }
        }

        return trimmed;
    }
}

public sealed record DesktopSignInError(
    string Code,
    string Message);

public enum DesktopSignInStatus
{
    NotStarted,
    Succeeded,
    Rejected,
    Failed,
    Unavailable
}