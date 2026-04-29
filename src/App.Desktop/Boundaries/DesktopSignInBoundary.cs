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
        "Enter login and password to sign in.");

    public static DesktopSignInResult InProgress { get; } = new(
        DesktopSignInStatus.InProgress,
        session: null,
        error: null,
        "Signing in...");

    public static DesktopSignInResult Succeeded(DesktopSessionSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new DesktopSignInResult(
            DesktopSignInStatus.Succeeded,
            session,
            error: null,
            CreateSuccessMessage(session));
    }

    public static DesktopSignInResult Rejected(string? code)
    {
        return WithError(
            DesktopSignInStatus.Rejected,
            code,
            fallbackCode: "sign_in_rejected",
            message: CreateErrorMessage(
                DesktopSignInStatus.Rejected,
                code,
                "sign_in_rejected"));
    }

    public static DesktopSignInResult Failed(string? code)
    {
        return WithError(
            DesktopSignInStatus.Failed,
            code,
            fallbackCode: "sign_in_failed",
            message: CreateErrorMessage(
                DesktopSignInStatus.Failed,
                code,
                "sign_in_failed"));
    }

    public static DesktopSignInResult Unavailable(string? code)
    {
        return WithError(
            DesktopSignInStatus.Unavailable,
            code,
            fallbackCode: "sign_in_unavailable",
            message: CreateErrorMessage(
                DesktopSignInStatus.Unavailable,
                code,
                "sign_in_unavailable"));
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

    private static string CreateSuccessMessage(DesktopSessionSnapshot session)
    {
        if (TryCreateSafeDisplayName(session.DisplayName, out string? displayName))
        {
            return $"Signed in as {displayName}.";
        }

        return "Signed in.";
    }

    private static bool TryCreateSafeDisplayName(string? value, out string? displayName)
    {
        displayName = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 80)
        {
            return false;
        }

        foreach (char character in trimmed)
        {
            if (char.IsControl(character))
            {
                return false;
            }
        }

        string lower = trimmed.ToLowerInvariant();
        string[] blockedFragments =
        [
            "authorization",
            "bearer",
            "password",
            "accesstoken",
            "access_token",
            "refresh_token",
            "refreshtoken"
        ];

        foreach (string blockedFragment in blockedFragments)
        {
            if (lower.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        displayName = trimmed;
        return true;
    }

    private static string CreateErrorMessage(
        DesktopSignInStatus status,
        string? code,
        string fallbackCode)
    {
        string normalizedCode = NormalizeCode(code, fallbackCode);
        if (string.Equals(normalizedCode, "missing_credentials", StringComparison.Ordinal))
        {
            return "Enter login and password to sign in.";
        }

        return status switch
        {
            DesktopSignInStatus.Rejected => "Login or password was not accepted.",
            DesktopSignInStatus.Unavailable => "Sign-in service is unavailable. Check connection or configuration.",
            _ => "Sign-in could not be completed. Try again."
        };
    }
}

public sealed record DesktopSignInError(
    string Code,
    string Message);

public enum DesktopSignInStatus
{
    NotStarted,
    InProgress,
    Succeeded,
    Rejected,
    Failed,
    Unavailable
}