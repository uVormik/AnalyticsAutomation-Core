namespace App.Desktop.Boundaries;

public interface IDesktopSignInService
{
    ValueTask<DesktopSignInResult> SignInAsync(
        string login,
        string password,
        Guid? deviceId,
        CancellationToken cancellationToken);
}

public static class DesktopSignInText
{
    public const string HeaderStatus = "Вход";
    public const string HeaderStatusBusy = "Выполняется вход";
    public const string HeaderStatusSignedIn = "Вход выполнен";
    public const string Title = "Вход в систему";
    public const string LoginLabel = "Логин";
    public const string LoginPlaceholder = "Введите логин";
    public const string PasswordLabel = "Пароль";
    public const string PasswordPlaceholder = "Введите пароль";
    public const string SubmitButton = "Войти";
    public const string SubmitButtonBusy = "Выполняется вход...";
    public const string NotStartedMessage = "Введите логин и пароль для входа.";
    public const string RejectedMessage = "Логин или пароль не приняты.";
    public const string UnavailableMessage = "Сервис входа недоступен. Проверьте подключение или настройку.";
    public const string FailedMessage = "Не удалось выполнить вход. Попробуйте еще раз.";
    public const string SignedOutMessage = "Вы вышли из системы. Введите логин и пароль для входа.";

    public static string CreateSuccessMessage(DesktopSessionSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (TryCreateSafeDisplayName(session.DisplayName, out string? displayName))
        {
            return $"Вход выполнен: {displayName}.";
        }

        return "Вход выполнен.";
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
            "sessionid",
            "session_id",
            "password",
            "token",
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
}

public static class DesktopSignedInShellText
{
    public const string Title = "Рабочая область";
    public const string SignOutButton = "Выйти";
    public const string DeferredPlaceholderMessage = "Будет доступно в следующем approved desktop slice.";

    public static IReadOnlyList<DesktopNavigationPlaceholderCard> NavigationCards { get; } =
    [
        new("Группы", DeferredPlaceholderMessage),
        new("Загрузка видео", DeferredPlaceholderMessage),
        new("Проверка перед загрузкой", DeferredPlaceholderMessage),
        new("Квитанции загрузки", DeferredPlaceholderMessage)
    ];

    public static string CreateUserContextMessage(DesktopSessionSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return DesktopSignInText.CreateSuccessMessage(session);
    }
}

public sealed record DesktopNavigationPlaceholderCard(
    string Title,
    string Message);

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
        DesktopSignInText.NotStartedMessage);

    public static DesktopSignInResult SignedOut { get; } = new(
        DesktopSignInStatus.NotStarted,
        session: null,
        error: null,
        DesktopSignInText.SignedOutMessage);

    public static DesktopSignInResult InProgress { get; } = new(
        DesktopSignInStatus.InProgress,
        session: null,
        error: null,
        DesktopSignInText.SubmitButtonBusy);

    public static DesktopSignInResult Succeeded(DesktopSessionSnapshot session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new DesktopSignInResult(
            DesktopSignInStatus.Succeeded,
            session,
            error: null,
            DesktopSignInText.CreateSuccessMessage(session));
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

    private static string CreateErrorMessage(
        DesktopSignInStatus status,
        string? code,
        string fallbackCode)
    {
        string normalizedCode = NormalizeCode(code, fallbackCode);
        if (string.Equals(normalizedCode, "missing_credentials", StringComparison.Ordinal))
        {
            return DesktopSignInText.NotStartedMessage;
        }

        return status switch
        {
            DesktopSignInStatus.Rejected => DesktopSignInText.RejectedMessage,
            DesktopSignInStatus.Unavailable => DesktopSignInText.UnavailableMessage,
            _ => DesktopSignInText.FailedMessage
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