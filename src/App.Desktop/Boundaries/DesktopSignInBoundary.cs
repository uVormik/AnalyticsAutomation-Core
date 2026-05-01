using System.IO;

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
        new("Загрузка видео", DesktopUploadSectionText.NavigationCardMessage, DesktopNavigationCardTarget.UploadSection),
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
    string Message,
    DesktopNavigationCardTarget Target = DesktopNavigationCardTarget.Deferred);

public enum DesktopNavigationCardTarget
{
    Deferred,
    UploadSection
}

public enum DesktopWorkspaceSection
{
    Workspace,
    Upload
}

public static class DesktopUploadSectionText
{
    public const string Title = "Загрузка видео";
    public const string Description =
        "Этот раздел показывает безопасные сведения о выбранном видеофайле. Реальная загрузка будет включена в следующем approved desktop slice.";
    public const string NavigationCardMessage = "Открыть заготовку выбора видеофайла.";
    public const string StepOneTitle = "Шаг 1. Метаданные файла";
    public const string SelectVideoFileButton = "Выбрать видеофайл";
    public const string PlaceholderResult = "Выберите видеофайл для безопасного предпросмотра.";
    public const string SelectingFileMessage = "Открывается выбор видеофайла.";
    public const string SelectionCanceledMessage = "Выбор файла отменён.";
    public const string SelectedFilePreviewMessage = "Файл выбран. Показаны только безопасные сведения.";
    public const string SelectionUnavailableMessage = "Не удалось получить безопасные сведения о файле.";
    public const string BackToWorkspaceButton = "Назад к рабочей области";
    public const string SelectedFileNameLabel = "Файл";
    public const string SelectedFileSizeLabel = "Размер";
    public const string SelectedFileContentTypeLabel = "Тип содержимого";
    public const string UnknownContentTypeValue = "Не определён";
    public const string StepTwoTitle = "Шаг 2. SHA-256";
    public const string CalculateSha256Button = "Рассчитать SHA-256";
    public const string CalculateSha256BusyButton = "Рассчитывается...";
    public const string HashNotReadyMessage = "Выберите видеофайл, чтобы рассчитать SHA-256.";
    public const string HashReadyMessage = "Файл выбран. Можно рассчитать SHA-256 локально на этом компьютере.";
    public const string HashInProgressMessage = "Рассчитывается SHA-256. Файл читается только в локальной desktop-границе.";
    public const string HashSucceededMessage = "SHA-256 рассчитан локально.";
    public const string HashUnavailableMessage = "Не удалось рассчитать SHA-256 для выбранного файла.";
    public const string HashCanceledMessage = "Расчёт SHA-256 отменён.";
    public const string Sha256ResultLabel = "SHA-256";
    public const string StepThreeTitle = "Шаг 3. Бизнес-объект";
    public const string BusinessObjectKeyLabel = "Ключ бизнес-объекта";
    public const string BusinessObjectKeyHint = "Временный ручной ввод для desktop prototype. Production-источник будет утверждён отдельным slice.";
    public const string BusinessObjectKeyApplyButton = "Проверить ключ";
    public const string BusinessObjectKeyUseFakeButton = "Подставить dev-ключ";
    public const string BusinessObjectKeyEmptyValidationMessage = "Введите ключ бизнес-объекта.";
    public const string BusinessObjectKeyControlCharacterValidationMessage =
        "Ключ бизнес-объекта не должен содержать переносы строк или управляющие символы.";
    public const string BusinessObjectKeyLengthValidationMessage =
        "Ключ бизнес-объекта должен быть от 1 до 128 символов.";
    public const string BusinessObjectKeySecretValidationMessage =
        "Ключ бизнес-объекта не должен содержать секретные значения.";
    public const string BusinessObjectKeyAcceptedMessage =
        "Ключ бизнес-объекта принят для безопасного preview.";
    public const string BusinessObjectKeyPreviewLabel = "businessObjectKey";
    public const string StepFourTitle = "Шаг 4. Предварительная проверка";
    public const string PreUploadCheckNotReadyMessage =
        "Выберите файл, рассчитайте SHA-256 и укажите ключ бизнес-объекта для preview запроса.";
    public const string PreUploadCheckReadyMessage =
        "Preview запроса готов. Реальный вызов App.Api в этом slice не выполняется.";
    public const string PreUploadCheckInProgressMessage =
        "Выполняется предварительная проверка в desktop-local dev boundary.";
    public const string PreUploadCheckDeferredMessage =
        "Предварительная проверка пока доступна только в dev-smoke режиме. Реальный вызов App.Api будет добавлен отдельным approved slice.";
    public const string PreUploadCheckAllowedDevMessage =
        "Предварительная проверка: загрузка разрешена в dev-smoke режиме.";
    public const string PreUploadCheckAllowWithReviewDevMessage =
        "Предварительная проверка: загрузка разрешена с review в dev-smoke режиме.";
    public const string PreUploadCheckBlockHardDuplicateDevMessage =
        "Предварительная проверка: загрузка заблокирована как hard duplicate в dev-smoke режиме.";
    public const string PreUploadCheckBlockPossibleFalsificationDevMessage =
        "Предварительная проверка: загрузка заблокирована как possible falsification в dev-smoke режиме.";
    public const string PreUploadCheckCanceledMessage = "Предварительная проверка отменена.";
    public const string PreUploadCheckButton = "Проверить перед загрузкой";
    public const string PreUploadCheckBusyButton = "Проверяется...";
    public const string PreUploadCheckFileNameLabel = "Имя файла";
    public const string PreUploadCheckFileSizeLabel = "Размер";
    public const string PreUploadCheckContentTypeLabel = "Тип содержимого";
    public const string PreUploadCheckSha256Label = "SHA-256";
    public const string PreUploadCheckBusinessObjectKeyLabel = "businessObjectKey";
    public const string PreUploadCheckCapturedAtUtcLabel = "capturedAtUtc";
    public const string PreUploadCheckDecisionLabel = "Решение";
    public const string StepFiveTitle = "Шаг 5. Загрузка на сайт";
    public const string SiteUploadNotReadyMessage =
        "Нужны выбранный файл, SHA-256, businessObjectKey и решение ALLOW или ALLOW_WITH_REVIEW.";
    public const string SiteUploadReadyMessage =
        "Preview загрузки на сайт готов. В этом slice доступен только desktop-local dev boundary.";
    public const string SiteUploadBlockedByPreUploadCheckMessage =
        "Загрузка на сайт недоступна для блокирующего решения предварительной проверки.";
    public const string SiteUploadInProgressMessage =
        "Выполняется загрузка на сайт в desktop-local dev boundary.";
    public const string SiteUploadDeferredMessage =
        "Загрузка на сайт пока доступна только в dev-smoke режиме. Реальный provider будет добавлен отдельным approved slice.";
    public const string SiteUploadSuccessDevMessage =
        "Загрузка на сайт выполнена в dev-smoke режиме.";
    public const string SiteUploadCanceledMessage = "Загрузка на сайт отменена.";
    public const string SiteUploadButton = "Загрузить на сайт";
    public const string SiteUploadBusyButton = "Загружается...";
    public const string SiteUploadFileNameLabel = "Имя файла";
    public const string SiteUploadFileSizeLabel = "Размер";
    public const string SiteUploadContentTypeLabel = "Тип содержимого";
    public const string SiteUploadSha256Label = "SHA-256";
    public const string SiteUploadBusinessObjectKeyLabel = "businessObjectKey";
    public const string SiteUploadPreUploadCheckDecisionLabel = "Решение PreUploadCheck";
    public const string SiteUploadCapturedAtUtcLabel = "capturedAtUtc";
    public const string SiteUploadResultStatusLabel = "status";
    public const string SiteUploadExternalVideoIdLabel = "externalVideoId";
    public const string SiteUploadSiteStorageKeyLabel = "siteStorageKey";
    public const string StepSixTitle = "Шаг 6. Квитанция загрузки";
    public const string UploadReceiptNotReadyMessage =
        "Нужны выбранный файл, SHA-256, businessObjectKey, решение ALLOW или ALLOW_WITH_REVIEW и успешная загрузка на сайт.";
    public const string UploadReceiptReadyMessage =
        "Preview квитанции загрузки готов. В этом slice доступен только desktop-local dev boundary.";
    public const string UploadReceiptInProgressMessage =
        "Формируется квитанция загрузки в desktop-local dev boundary.";
    public const string UploadReceiptDeferredMessage =
        "Квитанция загрузки пока доступна только в dev-smoke режиме. Реальный вызов App.Api будет добавлен отдельным approved slice.";
    public const string UploadReceiptAcceptedDevMessage =
        "Квитанция загрузки сформирована в dev-smoke режиме.";
    public const string UploadReceiptCanceledMessage = "Формирование квитанции загрузки отменено.";
    public const string UploadReceiptButton = "Сформировать квитанцию";
    public const string UploadReceiptBusyButton = "Формируется...";
    public const string UploadReceiptFileNameLabel = "Имя файла";
    public const string UploadReceiptFileSizeLabel = "Размер";
    public const string UploadReceiptContentTypeLabel = "Тип содержимого";
    public const string UploadReceiptSha256Label = "SHA-256";
    public const string UploadReceiptBusinessObjectKeyLabel = "businessObjectKey";
    public const string UploadReceiptPreUploadCheckDecisionLabel = "Решение PreUploadCheck";
    public const string UploadReceiptExternalVideoIdLabel = "externalVideoId";
    public const string UploadReceiptSiteStorageKeyLabel = "siteStorageKey";
    public const string UploadReceiptSiteUploadStatusLabel = "status загрузки на сайт";
    public const string UploadReceiptCapturedAtUtcLabel = "capturedAtUtc";
    public const string UploadReceiptResultStatusLabel = "status";
    public const string UploadReceiptResultReceiptIdLabel = "receiptId";
    public const string UploadReceiptResultServerCorrelationIdLabel = "serverCorrelationId";
    public const string NextStepDeferredMessage =
        "Следующий шаг отложен: реальный control-plane UploadReceipt client будет добавлен отдельным approved desktop slice.";
}

public sealed record DesktopUploadSelectedFile(
    string FileName,
    long SizeBytes,
    string ContentType)
{
    public const string VisualSmokeFileName = "visual-smoke-video.mp4";
    public const long VisualSmokeFileSizeBytes = 12345678;
    public const string VisualSmokeContentType = "video/mp4";

    public static DesktopUploadSelectedFile VisualSmokeFile { get; } = new(
        VisualSmokeFileName,
        VisualSmokeFileSizeBytes,
        VisualSmokeContentType);

    public static DesktopUploadSelectedFile FromSafeMetadata(
        string fileNameOrPath,
        long sizeBytes,
        string? contentType)
    {
        return new DesktopUploadSelectedFile(
            CreateSafeFileName(fileNameOrPath),
            Math.Max(0, sizeBytes),
            CreateSafeContentType(contentType));
    }

    private static string CreateSafeFileName(string? fileNameOrPath)
    {
        string? fileName = Path.GetFileName(fileNameOrPath?.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "selected-video-file";
        }

        var safeCharacters = new char[fileName.Length];
        var safeLength = 0;

        foreach (char character in fileName)
        {
            if (safeLength >= 160)
            {
                break;
            }

            if (char.IsControl(character)
                || character == Path.DirectorySeparatorChar
                || character == Path.AltDirectorySeparatorChar
                || character == Path.PathSeparator
                || character == Path.VolumeSeparatorChar)
            {
                continue;
            }

            safeCharacters[safeLength] = character;
            safeLength++;
        }

        string safeFileName = new string(safeCharacters, 0, safeLength).Trim();
        return string.IsNullOrWhiteSpace(safeFileName)
            ? "selected-video-file"
            : safeFileName;
    }

    private static string CreateSafeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return DesktopUploadSectionText.UnknownContentTypeValue;
        }

        string trimmed = contentType.Trim().ToLowerInvariant();
        if (trimmed.Length > 80 || trimmed.Count(value => value == '/') != 1)
        {
            return DesktopUploadSectionText.UnknownContentTypeValue;
        }

        string[] blockedFragments =
        [
            "authorization",
            "bearer",
            "password",
            "token",
            "sessionid",
            "session_id",
            "access_token",
            "refreshtoken",
            "refresh_token"
        ];

        foreach (string blockedFragment in blockedFragments)
        {
            if (trimmed.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return DesktopUploadSectionText.UnknownContentTypeValue;
            }
        }

        foreach (char character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '/' and not '+' and not '-' and not '.')
            {
                return DesktopUploadSectionText.UnknownContentTypeValue;
            }
        }

        return trimmed;
    }
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