using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

namespace App.Desktop.Tests;

public sealed class DesktopUploadSectionTests
{
    [Fact]
    public void UploadCardIsActiveNavigationEntryAfterSignedInShell()
    {
        DesktopNavigationPlaceholderCard uploadCard = Assert.Single(
            DesktopSignedInShellText.NavigationCards,
            card => string.Equals(card.Title, DesktopUploadSectionText.Title, StringComparison.Ordinal));

        Assert.Equal(DesktopNavigationCardTarget.UploadSection, uploadCard.Target);
        Assert.Equal("Открыть заготовку выбора видеофайла.", uploadCard.Message);
    }

    [Fact]
    public void UploadCardNavigationOpensUploadSection()
    {
        var viewModel = CreateViewModel(new CancelingDesktopVideoFilePicker());

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);

        viewModel.OpenUploadSection();

        Assert.Equal(DesktopWorkspaceSection.Upload, viewModel.CurrentSection);
        Assert.True(viewModel.IsUploadSectionOpen);
    }

    [Fact]
    public void UploadSectionHasRequiredRussianPlaceholderText()
    {
        Assert.Equal("Загрузка видео", DesktopUploadSectionText.Title);
        Assert.Equal(
            "Этот раздел показывает безопасные сведения о выбранном видеофайле. Реальная загрузка будет включена в следующем approved desktop slice.",
            DesktopUploadSectionText.Description);
        Assert.Equal("Контекст группы", DesktopUploadSectionText.GroupContextTitle);
        Assert.Equal(
            "Группа не выбрана. Для production-flow выбор группы будет обязательным в отдельном slice.",
            DesktopUploadSectionText.GroupContextMissingMessage);
        Assert.Equal("Группа", DesktopUploadSectionText.GroupContextNameLabel);
        Assert.Equal("safe key/id preview", DesktopUploadSectionText.GroupContextIdLabel);
        Assert.Equal("Шаг 1. Метаданные файла", DesktopUploadSectionText.StepOneTitle);
        Assert.Equal("Выбрать видеофайл", DesktopUploadSectionText.SelectVideoFileButton);
        Assert.Equal(
            "Выберите видеофайл для безопасного предпросмотра.",
            DesktopUploadSectionText.PlaceholderResult);
        Assert.Equal("Открывается выбор видеофайла.", DesktopUploadSectionText.SelectingFileMessage);
        Assert.Equal("Выбор файла отменён.", DesktopUploadSectionText.SelectionCanceledMessage);
        Assert.Equal(
            "Файл выбран. Показаны только безопасные сведения.",
            DesktopUploadSectionText.SelectedFilePreviewMessage);
        Assert.Equal(
            "Не удалось получить безопасные сведения о файле.",
            DesktopUploadSectionText.SelectionUnavailableMessage);
        Assert.Equal("Назад к рабочей области", DesktopUploadSectionText.BackToWorkspaceButton);
        Assert.Equal("Шаг 2. SHA-256", DesktopUploadSectionText.StepTwoTitle);
        Assert.Equal("Рассчитать SHA-256", DesktopUploadSectionText.CalculateSha256Button);
        Assert.Equal("Рассчитывается...", DesktopUploadSectionText.CalculateSha256BusyButton);
        Assert.Equal("Выберите видеофайл, чтобы рассчитать SHA-256.", DesktopUploadSectionText.HashNotReadyMessage);
        Assert.Equal("Файл выбран. Можно рассчитать SHA-256 локально на этом компьютере.", DesktopUploadSectionText.HashReadyMessage);
        Assert.Equal("Рассчитывается SHA-256. Файл читается только в локальной desktop-границе.", DesktopUploadSectionText.HashInProgressMessage);
        Assert.Equal("SHA-256 рассчитан локально.", DesktopUploadSectionText.HashSucceededMessage);
        Assert.Equal("Не удалось рассчитать SHA-256 для выбранного файла.", DesktopUploadSectionText.HashUnavailableMessage);
        Assert.Equal("Расчёт SHA-256 отменён.", DesktopUploadSectionText.HashCanceledMessage);
        Assert.Equal("SHA-256", DesktopUploadSectionText.Sha256ResultLabel);
        Assert.Equal("Шаг 3. Бизнес-объект", DesktopUploadSectionText.StepThreeTitle);
        Assert.Equal("Ключ бизнес-объекта", DesktopUploadSectionText.BusinessObjectKeyLabel);
        Assert.Equal(
            "Временный ручной ввод для desktop prototype. Production-источник будет утверждён отдельным slice.",
            DesktopUploadSectionText.BusinessObjectKeyHint);
        Assert.Equal("Проверить ключ", DesktopUploadSectionText.BusinessObjectKeyApplyButton);
        Assert.Equal("Подставить dev-ключ", DesktopUploadSectionText.BusinessObjectKeyUseFakeButton);
        Assert.Equal("Введите ключ бизнес-объекта.", DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage);
        Assert.Equal(
            "Ключ бизнес-объекта не должен содержать переносы строк или управляющие символы.",
            DesktopUploadSectionText.BusinessObjectKeyControlCharacterValidationMessage);
        Assert.Equal(
            "Ключ бизнес-объекта должен быть от 1 до 128 символов.",
            DesktopUploadSectionText.BusinessObjectKeyLengthValidationMessage);
        Assert.Equal(
            "Ключ бизнес-объекта не должен содержать секретные значения.",
            DesktopUploadSectionText.BusinessObjectKeySecretValidationMessage);
        Assert.Equal(
            "Ключ бизнес-объекта принят для безопасного preview.",
            DesktopUploadSectionText.BusinessObjectKeyAcceptedMessage);
        Assert.Equal("businessObjectKey", DesktopUploadSectionText.BusinessObjectKeyPreviewLabel);
        Assert.Equal("Шаг 4. Предварительная проверка", DesktopUploadSectionText.StepFourTitle);
        Assert.Equal(
            "Выберите файл, рассчитайте SHA-256 и укажите ключ бизнес-объекта для preview запроса.",
            DesktopUploadSectionText.PreUploadCheckNotReadyMessage);
        Assert.Equal(
            "Preview запроса готов. Реальный вызов App.Api в этом slice не выполняется.",
            DesktopUploadSectionText.PreUploadCheckReadyMessage);
        Assert.Equal(
            "Выполняется предварительная проверка в desktop-local dev boundary.",
            DesktopUploadSectionText.PreUploadCheckInProgressMessage);
        Assert.Equal(
            "Предварительная проверка недоступна: задайте live control-plane base address или включите dev-smoke guard.",
            DesktopUploadSectionText.PreUploadCheckDeferredMessage);
        Assert.Equal(
            "Предварительная проверка: загрузка разрешена в dev-smoke режиме.",
            DesktopUploadSectionText.PreUploadCheckAllowedDevMessage);
        Assert.Equal("Проверить перед загрузкой", DesktopUploadSectionText.PreUploadCheckButton);
        Assert.Equal("Проверяется...", DesktopUploadSectionText.PreUploadCheckBusyButton);
        Assert.Equal("Имя файла", DesktopUploadSectionText.PreUploadCheckFileNameLabel);
        Assert.Equal("Размер", DesktopUploadSectionText.PreUploadCheckFileSizeLabel);
        Assert.Equal("Тип содержимого", DesktopUploadSectionText.PreUploadCheckContentTypeLabel);
        Assert.Equal("SHA-256", DesktopUploadSectionText.PreUploadCheckSha256Label);
        Assert.Equal("businessObjectKey", DesktopUploadSectionText.PreUploadCheckBusinessObjectKeyLabel);
        Assert.Equal("capturedAtUtc", DesktopUploadSectionText.PreUploadCheckCapturedAtUtcLabel);
        Assert.Equal("Решение", DesktopUploadSectionText.PreUploadCheckDecisionLabel);
        Assert.Equal("Шаг 5. Загрузка на сайт", DesktopUploadSectionText.StepFiveTitle);
        Assert.Equal(
            "Нужны выбранный файл, SHA-256, businessObjectKey и решение ALLOW или ALLOW_WITH_REVIEW.",
            DesktopUploadSectionText.SiteUploadNotReadyMessage);
        Assert.Equal(
            "Preview загрузки на сайт готов. В этом slice доступен только desktop-local dev boundary.",
            DesktopUploadSectionText.SiteUploadReadyMessage);
        Assert.Equal(
            "Загрузка на сайт недоступна для блокирующего решения предварительной проверки.",
            DesktopUploadSectionText.SiteUploadBlockedByPreUploadCheckMessage);
        Assert.Equal(
            "Выполняется загрузка на сайт в desktop-local dev boundary.",
            DesktopUploadSectionText.SiteUploadInProgressMessage);
        Assert.Equal(
            "Загрузка на сайт пока доступна только в dev-smoke режиме. Реальный provider будет добавлен отдельным approved slice.",
            DesktopUploadSectionText.SiteUploadDeferredMessage);
        Assert.Equal(
            "Загрузка на сайт выполнена в dev-smoke режиме.",
            DesktopUploadSectionText.SiteUploadSuccessDevMessage);
        Assert.Equal("Загрузка на сайт отменена.", DesktopUploadSectionText.SiteUploadCanceledMessage);
        Assert.Equal("Загрузить на сайт", DesktopUploadSectionText.SiteUploadButton);
        Assert.Equal("Загружается...", DesktopUploadSectionText.SiteUploadBusyButton);
        Assert.Equal("Имя файла", DesktopUploadSectionText.SiteUploadFileNameLabel);
        Assert.Equal("Размер", DesktopUploadSectionText.SiteUploadFileSizeLabel);
        Assert.Equal("Тип содержимого", DesktopUploadSectionText.SiteUploadContentTypeLabel);
        Assert.Equal("SHA-256", DesktopUploadSectionText.SiteUploadSha256Label);
        Assert.Equal("businessObjectKey", DesktopUploadSectionText.SiteUploadBusinessObjectKeyLabel);
        Assert.Equal("Решение PreUploadCheck", DesktopUploadSectionText.SiteUploadPreUploadCheckDecisionLabel);
        Assert.Equal("capturedAtUtc", DesktopUploadSectionText.SiteUploadCapturedAtUtcLabel);
        Assert.Equal("status", DesktopUploadSectionText.SiteUploadResultStatusLabel);
        Assert.Equal("externalVideoId", DesktopUploadSectionText.SiteUploadExternalVideoIdLabel);
        Assert.Equal("siteStorageKey", DesktopUploadSectionText.SiteUploadSiteStorageKeyLabel);
        Assert.Equal("Шаг 6. Квитанция загрузки", DesktopUploadSectionText.StepSixTitle);
        Assert.Equal(
            "Нужны выбранный файл, SHA-256, businessObjectKey, решение ALLOW или ALLOW_WITH_REVIEW и успешная загрузка на сайт.",
            DesktopUploadSectionText.UploadReceiptNotReadyMessage);
        Assert.Equal(
            "Preview квитанции загрузки готов. В этом slice доступен только desktop-local dev boundary.",
            DesktopUploadSectionText.UploadReceiptReadyMessage);
        Assert.Equal(
            "Preview квитанции загрузки готов. Будет использован существующий control-plane endpoint UploadReceipt.",
            DesktopUploadSectionText.UploadReceiptLiveReadyMessage);
        Assert.Equal(
            "Формируется квитанция загрузки в desktop-local dev boundary.",
            DesktopUploadSectionText.UploadReceiptInProgressMessage);
        Assert.Equal(
            "Формируется квитанция загрузки через control-plane boundary.",
            DesktopUploadSectionText.UploadReceiptLiveInProgressMessage);
        Assert.Equal(
            "Квитанция загрузки недоступна: задайте live control-plane base address или включите dev-smoke guard.",
            DesktopUploadSectionText.UploadReceiptDeferredMessage);
        Assert.Equal(
            "Квитанция загрузки сформирована в dev-smoke режиме.",
            DesktopUploadSectionText.UploadReceiptAcceptedDevMessage);
        Assert.Equal(
            "Квитанция загрузки принята control-plane.",
            DesktopUploadSectionText.UploadReceiptAcceptedLiveMessage);
        Assert.Equal(
            "Квитанция загрузки уже была принята control-plane.",
            DesktopUploadSectionText.UploadReceiptAlreadyAcceptedLiveMessage);
        Assert.Equal(
            "Квитанция загрузки недоступна: требуется повторный вход.",
            DesktopUploadSectionText.UploadReceiptLiveUnauthorizedMessage);
        Assert.Equal(
            "Квитанция загрузки временно недоступна. Проверьте подключение или настройку.",
            DesktopUploadSectionText.UploadReceiptLiveUnavailableMessage);
        Assert.Equal(
            "Квитанция загрузки не принята. Проверьте состояние загрузки и попробуйте ещё раз.",
            DesktopUploadSectionText.UploadReceiptLiveFailedMessage);
        Assert.Equal(
            "Квитанция загрузки вернула неподдерживаемый ответ.",
            DesktopUploadSectionText.UploadReceiptLiveMalformedMessage);
        Assert.Equal("Формирование квитанции загрузки отменено.", DesktopUploadSectionText.UploadReceiptCanceledMessage);
        Assert.Equal("Сформировать квитанцию", DesktopUploadSectionText.UploadReceiptButton);
        Assert.Equal("Формируется...", DesktopUploadSectionText.UploadReceiptBusyButton);
        Assert.Equal("Имя файла", DesktopUploadSectionText.UploadReceiptFileNameLabel);
        Assert.Equal("Размер", DesktopUploadSectionText.UploadReceiptFileSizeLabel);
        Assert.Equal("Тип содержимого", DesktopUploadSectionText.UploadReceiptContentTypeLabel);
        Assert.Equal("SHA-256", DesktopUploadSectionText.UploadReceiptSha256Label);
        Assert.Equal("businessObjectKey", DesktopUploadSectionText.UploadReceiptBusinessObjectKeyLabel);
        Assert.Equal("Решение PreUploadCheck", DesktopUploadSectionText.UploadReceiptPreUploadCheckDecisionLabel);
        Assert.Equal("externalVideoId", DesktopUploadSectionText.UploadReceiptExternalVideoIdLabel);
        Assert.Equal("siteStorageKey", DesktopUploadSectionText.UploadReceiptSiteStorageKeyLabel);
        Assert.Equal("status загрузки на сайт", DesktopUploadSectionText.UploadReceiptSiteUploadStatusLabel);
        Assert.Equal("capturedAtUtc", DesktopUploadSectionText.UploadReceiptCapturedAtUtcLabel);
        Assert.Equal("status", DesktopUploadSectionText.UploadReceiptResultStatusLabel);
        Assert.Equal("receiptId", DesktopUploadSectionText.UploadReceiptResultReceiptIdLabel);
        Assert.Equal("serverCorrelationId", DesktopUploadSectionText.UploadReceiptResultServerCorrelationIdLabel);
        Assert.Equal(
            "Следующий шаг отложен: production site provider остаётся отдельным approved desktop slice.",
            DesktopUploadSectionText.NextStepDeferredMessage);
    }

    [Fact]
    public void UploadSectionDoesNotSendVideoBytesOrUseLegacyUploadOrchestrationBoundaries()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Components", "DesktopShell.razor"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionViewModel.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionOptions.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopVideoFilePicker.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopVideoHashService.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopVideoHashService.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadBusinessObjectKey.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopPreUploadCheck.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DisabledDesktopPreUploadCheckClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopPreUploadCheckClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopSiteUpload.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DisabledDesktopDirectSiteUploadClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopDirectSiteUploadClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadReceipt.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DisabledDesktopUploadReceiptClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopUploadReceiptClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "WpfDesktopVideoFilePicker.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopVideoFilePickerBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopVideoHashBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopPreUploadCheckBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopDirectSiteUploadBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopUploadReceiptBoundary.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("App.Api", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IControlPlaneApiClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IDesktopUploadOrchestrator", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RequestPreUploadCheckAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ControlPlanePreUploadCheckRequest", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RecordUploadReceiptAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ControlPlaneUploadReceiptDraft", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAllBytes", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ReadAllBytesAsync", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PreUploadCheckClientUsesOnlyApprovedEndpointAndDoesNotReadRawBodiesOrBytes()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopPreUploadCheck.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DisabledDesktopPreUploadCheckClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopPreUploadCheckClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "HttpDesktopPreUploadCheckClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopPreUploadCheckBoundary.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("ReadAsStringAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAllBytes", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ReadAllBytesAsync", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("WriteLine", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Log", source, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/video/upload-receipt-sync", source, StringComparison.Ordinal);
        }

        string liveClientSource = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "HttpDesktopPreUploadCheckClient.cs"));

        Assert.Contains("/api/video/pre-upload-check", liveClientSource, StringComparison.Ordinal);
        Assert.Contains("/api/video/upload-receipt", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/video/pre-upload-check/", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/video/upload-receipt/", liveClientSource, StringComparison.Ordinal);
    }

    [Fact]
    public void UploadReceiptClientUsesOnlyApprovedEndpointAndDoesNotReadRawBodiesOrBytes()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadReceipt.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DisabledDesktopUploadReceiptClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopUploadReceiptClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "HttpDesktopUploadReceiptClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopUploadReceiptBoundary.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("ReadAsStringAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadAllBytes", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ReadAllBytesAsync", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("WriteLine", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Log", source, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/video/pre-upload-check", source, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/video/upload-receipt-sync", source, StringComparison.Ordinal);
        }

        string liveClientSource = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "HttpDesktopUploadReceiptClient.cs"));

        Assert.Contains("/api/video/upload-receipt", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/video/upload-receipt/", liveClientSource, StringComparison.Ordinal);
    }

    [Fact]
    public void FakeUploadFileSelectionHashBusinessObjectKeyPreUploadCheckSiteUploadAndUploadReceiptAreDevOnlyAndDisabledByDefault()
    {
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsLiveControlPlaneUploadReceiptEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeSiteUploadEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeUploadReceiptEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsLiveControlPlanePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsLiveControlPlaneUploadReceiptEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeSiteUploadEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeUploadReceiptEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false").IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false").IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false").IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false")
            .IsDevFakePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false")
            .IsLiveControlPlaneUploadReceiptEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false", "false")
            .IsDevFakeSiteUploadEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false", "false", "false")
            .IsDevFakeUploadReceiptEnabled);

#if DEBUG
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true").IsDevFakeUploadFileEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("false", "true").IsDevFakeUploadHashEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true").IsDevFakeUploadHashEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "true").IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true").IsDevFakeBusinessObjectKeyEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true")
            .IsDevFakePreUploadCheckEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true", "true")
            .IsDevFakePreUploadCheckEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false", "true")
            .IsDevFakeSiteUploadEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true", "true", "true")
            .IsDevFakeSiteUploadEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false", "false", "true")
            .IsDevFakeUploadReceiptEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true", "true", "true", "true")
            .IsDevFakeUploadReceiptEnabled);
#else
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true").IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "true").IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true").IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "true").IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true").IsDevFakeBusinessObjectKeyEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true")
            .IsDevFakePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true", "true")
            .IsDevFakePreUploadCheckEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false", "true")
            .IsDevFakeSiteUploadEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true", "true", "true")
            .IsDevFakeSiteUploadEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "false", "false", "true")
            .IsDevFakeUploadReceiptEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true", "true", "true", "true", "true")
            .IsDevFakeUploadReceiptEnabled);
#endif
    }

    [Fact]
    public void FakeBusinessObjectKeyIsDisabledByDefault()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        DesktopUploadBusinessObjectKeyValidation? validation = viewModel.UseDevFakeBusinessObjectKey();

        Assert.False(viewModel.IsDevFakeBusinessObjectKeyEnabled);
        Assert.Null(validation);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.False(viewModel.HasBusinessObjectKey);
        Assert.Equal(string.Empty, viewModel.BusinessObjectKeyInput);
    }

    [Fact]
    public void FakeBusinessObjectKeyUsesVisualSmokeValueWhenExplicitlyEnabled()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "true"));

        DesktopUploadBusinessObjectKeyValidation? validation = viewModel.UseDevFakeBusinessObjectKey();

#if DEBUG
        Assert.NotNull(validation);
        Assert.True(validation.IsValid);
        Assert.True(viewModel.IsDevFakeBusinessObjectKeyEnabled);
        Assert.Equal(DesktopUploadBusinessObjectKey.VisualSmokeValue, viewModel.BusinessObjectKeyInput);
        Assert.Equal("visual-smoke-business-object-001", viewModel.BusinessObjectKeyPreview);
        Assert.Equal(DesktopUploadSectionText.BusinessObjectKeyAcceptedMessage, viewModel.BusinessObjectKeyStatusMessage);
#else
        Assert.Null(validation);
        Assert.False(viewModel.IsDevFakeBusinessObjectKeyEnabled);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
#endif
    }

    [Fact]
    public void ManualBusinessObjectKeyTrimsWhitespaceAndShowsSafePreview()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());
        viewModel.BusinessObjectKeyInput = "  report-draft-001  ";

        DesktopUploadBusinessObjectKeyValidation validation = viewModel.ApplyBusinessObjectKey();

        Assert.True(validation.IsValid);
        Assert.Equal("report-draft-001", viewModel.BusinessObjectKeyInput);
        Assert.Equal("report-draft-001", viewModel.BusinessObjectKeyPreview);
        Assert.True(viewModel.HasBusinessObjectKey);
        Assert.Equal(DesktopUploadSectionText.BusinessObjectKeyAcceptedMessage, viewModel.BusinessObjectKeyStatusMessage);
    }

    [Fact]
    public void EmptyBusinessObjectKeyShowsSafeRussianValidationMessage()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());
        viewModel.BusinessObjectKeyInput = "   ";

        DesktopUploadBusinessObjectKeyValidation validation = viewModel.ApplyBusinessObjectKey();

        Assert.False(validation.IsValid);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.Equal(string.Empty, viewModel.BusinessObjectKeyInput);
        Assert.Equal(DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage, viewModel.BusinessObjectKeyStatusMessage);
    }

    [Fact]
    public void MultilineOrControlCharacterBusinessObjectKeyWithoutBlockedFragmentIsRejectedAndInputIsMadeSingleLine()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());
        viewModel.BusinessObjectKeyInput = "safe-key\r\nhidden";

        DesktopUploadBusinessObjectKeyValidation validation = viewModel.ApplyBusinessObjectKey();

        Assert.False(validation.IsValid);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.DoesNotContain("\r", viewModel.BusinessObjectKeyInput, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", viewModel.BusinessObjectKeyInput, StringComparison.Ordinal);
        Assert.Equal("safe-keyhidden", viewModel.BusinessObjectKeyInput);
        Assert.Equal(
            DesktopUploadSectionText.BusinessObjectKeyControlCharacterValidationMessage,
            viewModel.BusinessObjectKeyStatusMessage);
    }

    [Theory]
    [InlineData("tok\nen", "token")]
    [InlineData("ac\ncessToken", "accessToken")]
    [InlineData("refresh\rToken", "refreshToken")]
    [InlineData("authori\tzation", "authorization")]
    [InlineData("pass\nword", "password")]
    public void ControlCharacterBusinessObjectKeyWithBlockedFragmentIsRejectedAndCleared(
        string input,
        string blockedFragment)
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());
        viewModel.BusinessObjectKeyInput = input;

        DesktopUploadBusinessObjectKeyValidation validation = viewModel.ApplyBusinessObjectKey();

        Assert.False(validation.IsValid);
        Assert.Null(validation.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.False(viewModel.HasBusinessObjectKey);
        Assert.Equal(string.Empty, validation.SafeInputValue);
        Assert.Equal(string.Empty, viewModel.BusinessObjectKeyInput);
        Assert.Equal(
            DesktopUploadSectionText.BusinessObjectKeySecretValidationMessage,
            viewModel.BusinessObjectKeyStatusMessage);

        string visibleState = validation.SafeInputValue
            + " "
            + viewModel.BusinessObjectKeyInput
            + " "
            + (viewModel.BusinessObjectKeyPreview ?? string.Empty)
            + " "
            + viewModel.BusinessObjectKeyStatusMessage;
        Assert.DoesNotContain(blockedFragment, visibleState, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BusinessObjectKeyLengthLimitIsEnforced()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());
        viewModel.BusinessObjectKeyInput = new string('a', DesktopUploadBusinessObjectKey.MaxLength + 1);

        DesktopUploadBusinessObjectKeyValidation validation = viewModel.ApplyBusinessObjectKey();

        Assert.False(validation.IsValid);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.Equal(DesktopUploadBusinessObjectKey.MaxLength, viewModel.BusinessObjectKeyInput.Length);
        Assert.Equal(DesktopUploadSectionText.BusinessObjectKeyLengthValidationMessage, viewModel.BusinessObjectKeyStatusMessage);
    }

    [Fact]
    public void BusinessObjectKeyPreviewDoesNotContainTokensPasswordOrAuthorization()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());
        viewModel.BusinessObjectKeyInput = "Authorization-password-token";

        DesktopUploadBusinessObjectKeyValidation validation = viewModel.ApplyBusinessObjectKey();

        Assert.False(validation.IsValid);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.Equal(string.Empty, viewModel.BusinessObjectKeyInput);

        string visibleState = viewModel.BusinessObjectKeyStatusMessage
            + " "
            + (viewModel.BusinessObjectKeyPreview ?? string.Empty);
        Assert.DoesNotContain("password", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", visibleState, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelSelectionShowsSafeRussianMessage()
    {
        var viewModel = CreateViewModel(new CancelingDesktopVideoFilePicker());

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.Null(selectedFile);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.SelectionCanceledMessage, viewModel.SelectionStatusMessage);
        Assert.Equal(DesktopUploadSectionText.HashNotReadyMessage, viewModel.HashStatusMessage);
        Assert.False(viewModel.CanCalculateHash);
        Assert.False(viewModel.IsSelectingFile);
    }

    [Fact]
    public async Task FakeSelectedFileStateIsSafeAndContainsNoRealPathOrSecret()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.NotNull(selectedFile);
        Assert.Equal("visual-smoke-video.mp4", selectedFile.FileName);
        Assert.Equal(12345678, selectedFile.SizeBytes);
        Assert.Equal("video/mp4", selectedFile.ContentType);
        Assert.Equal(DesktopUploadSectionText.SelectedFilePreviewMessage, viewModel.SelectionStatusMessage);
        Assert.Equal(DesktopUploadSectionText.HashReadyMessage, viewModel.HashStatusMessage);
        Assert.True(viewModel.CanCalculateHash);
        Assert.False(viewModel.IsSelectingFile);

        string visibleState = selectedFile.ToString() + " " + viewModel.SelectionStatusMessage;
        Assert.DoesNotContain(@"\", visibleState, StringComparison.Ordinal);
        Assert.DoesNotContain("/", selectedFile.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain(":", visibleState, StringComparison.Ordinal);
        Assert.DoesNotContain("password", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("session_id", visibleState, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectedFilePreviewHidesPathSegments()
    {
        string nestedFileName = Path.Combine("operator-private", "nested", "safe-preview.mp4");
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(nestedFileName, 42, "video/mp4")));

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.NotNull(selectedFile);
        Assert.Equal("safe-preview.mp4", selectedFile.FileName);
        Assert.Equal(42, selectedFile.SizeBytes);
        Assert.Equal("video/mp4", selectedFile.ContentType);

        string visibleState = selectedFile.ToString() + " " + viewModel.SelectionStatusMessage;
        Assert.DoesNotContain("operator-private", visibleState, StringComparison.Ordinal);
        Assert.DoesNotContain("nested", visibleState, StringComparison.Ordinal);
        Assert.DoesNotContain(@"\", selectedFile.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("/", selectedFile.FileName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnsafeContentTypeFallsBackToUnknownSafeText()
    {
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected("safe-preview.mp4", 42, "Authorization/token")));

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.NotNull(selectedFile);
        Assert.Equal(DesktopUploadSectionText.UnknownContentTypeValue, selectedFile.ContentType);
        Assert.DoesNotContain("Authorization", selectedFile.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", selectedFile.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashActionIsUnavailableUntilFileSelected()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        Assert.False(viewModel.CanCalculateHash);
        Assert.Equal(DesktopUploadSectionText.HashNotReadyMessage, viewModel.HashStatusMessage);
        Assert.Null(viewModel.Sha256Hex);
    }

    [Fact]
    public async Task FakeHashReturnsExpectedLowercaseSha256Hex()
    {
        var result = await new FakeDesktopVideoHashService().CalculateSha256Async(
            DesktopVideoHashRequest.FromSource(DesktopVideoHashSource.VisualSmoke),
            CancellationToken.None);

        Assert.Equal(DesktopVideoHashStatus.Succeeded, result.Status);
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, result.Sha256Hex);
        string sha256Hex = result.Sha256Hex ?? throw new InvalidOperationException("SHA-256 was not returned.");
        Assert.Equal(64, sha256Hex.Length);
        Assert.Matches("^[0-9a-f]{64}$", sha256Hex);
    }

    [Fact]
    public async Task SuccessfulHashResultVisibleAfterSelectedFile()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.True(viewModel.CanCalculateHash);
        Assert.Equal(DesktopUploadSectionText.HashReadyMessage, viewModel.HashStatusMessage);

        DesktopVideoHashResult? result = await viewModel.CalculateSha256Async(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(DesktopVideoHashStatus.Succeeded, result.Status);
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, viewModel.Sha256Hex);
        Assert.True(viewModel.HasSha256Hash);
        Assert.False(viewModel.IsHashing);
        Assert.True(viewModel.CanCalculateHash);
        Assert.Equal(DesktopUploadSectionText.HashSucceededMessage, viewModel.HashStatusMessage);
    }

    [Fact]
    public async Task PreUploadCheckRequestPreviewRequiresSelectedFileSha256AndBusinessObjectKey()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        Assert.Null(viewModel.PreUploadCheckRequestPreview);
        Assert.False(viewModel.HasPreUploadCheckRequestPreview);
        Assert.False(viewModel.CanRequestPreUploadCheck);

        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.Null(viewModel.PreUploadCheckRequestPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckNotReadyMessage, viewModel.PreUploadCheckStatusMessage);

        _ = await viewModel.CalculateSha256Async(CancellationToken.None);

        Assert.Null(viewModel.PreUploadCheckRequestPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckNotReadyMessage, viewModel.PreUploadCheckStatusMessage);

        viewModel.BusinessObjectKeyInput = "report-draft-001";
        _ = viewModel.ApplyBusinessObjectKey();

        Assert.NotNull(viewModel.PreUploadCheckRequestPreview);
        Assert.True(viewModel.HasPreUploadCheckRequestPreview);
        Assert.True(viewModel.CanRequestPreUploadCheck);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckReadyMessage, viewModel.PreUploadCheckStatusMessage);
    }

    [Fact]
    public async Task PreUploadCheckRequestPreviewShowsOnlySafeFields()
    {
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(
                Path.Combine("private-folder", "nested", "safe-preview.mp4"),
                42,
                "video/mp4",
                DesktopVideoHashSource.VisualSmoke)));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, " report-draft-001 ");

        DesktopPreUploadCheckRequestPreview preview =
            viewModel.PreUploadCheckRequestPreview ?? throw new InvalidOperationException("Preview was not created.");

        Assert.Equal("safe-preview.mp4", preview.FileName);
        Assert.Equal(42, preview.SizeBytes);
        Assert.Equal("video/mp4", preview.ContentType);
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, preview.Sha256Hex);
        Assert.Equal("report-draft-001", preview.BusinessObjectKeyPreview);
        Assert.Equal(DesktopPreUploadCheckRequestPreview.CapturedAtUtcPlaceholder, preview.CapturedAtUtc);
    }

    [Fact]
    public async Task PreUploadCheckRequestPreviewDoesNotShowFullLocalPath()
    {
        string privateFolder = "operator-private-preupload-source";
        string localPath = Path.Combine(Path.GetTempPath(), privateFolder, "safe-preview.mp4");
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(
                localPath,
                42,
                "video/mp4",
                DesktopVideoHashSource.VisualSmoke)));

        DesktopPreUploadCheckRequestPreview preview = await PreparePreUploadCheckPreviewAsync(
            viewModel,
            "report-draft-001");

        string visibleState = preview.ToString()
            + " "
            + viewModel.PreUploadCheckStatusMessage
            + " "
            + (viewModel.PreUploadCheckDecisionPreview ?? string.Empty);

        Assert.Equal("safe-preview.mp4", preview.FileName);
        Assert.DoesNotContain(Path.GetTempPath(), visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(privateFolder, visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\", preview.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("/", preview.FileName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledPreUploadCheckShowsSafeDeferredMessageAndDoesNotCallBoundary()
    {
        var preUploadCheckClient = new ThrowingDesktopPreUploadCheckClient();
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            preUploadCheckClient,
            DesktopUploadSectionOptions.Disabled);

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");

        DesktopPreUploadCheckResult? result = await viewModel.CheckPreUploadAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(DesktopPreUploadCheckStatus.Deferred, result.Status);
        Assert.Null(result.Decision);
        Assert.Null(viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckDeferredMessage, viewModel.PreUploadCheckStatusMessage);
        Assert.Equal(0, preUploadCheckClient.CallCount);
    }

    [Fact]
    public async Task EnabledPreUploadCheckShowsFakeAllowDecision()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");

        DesktopPreUploadCheckResult? result = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(result);
        Assert.Equal(DesktopPreUploadCheckStatus.Allowed, result.Status);
        Assert.Equal(DesktopPreUploadCheckDecision.Allow, result.Decision);
        Assert.Equal("ALLOW", result.DecisionPreview);
        Assert.True(viewModel.HasPreUploadCheckDecision);
        Assert.Equal("ALLOW", viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckAllowedDevMessage, viewModel.PreUploadCheckStatusMessage);
#else
        Assert.NotNull(result);
        Assert.Equal(DesktopPreUploadCheckStatus.Deferred, result.Status);
        Assert.Null(viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckDeferredMessage, viewModel.PreUploadCheckStatusMessage);
#endif
    }

    [Theory]
    [InlineData(DesktopPreUploadCheckDecision.Allow, "ALLOW")]
    [InlineData(DesktopPreUploadCheckDecision.AllowWithReview, "ALLOW_WITH_REVIEW")]
    [InlineData(DesktopPreUploadCheckDecision.BlockHardDuplicate, "BLOCK_HARD_DUPLICATE")]
    [InlineData(DesktopPreUploadCheckDecision.BlockPossibleFalsification, "BLOCK_POSSIBLE_FALSIFICATION")]
    public async Task FakePreUploadCheckSupportsExpectedDevDecisionNames(
        DesktopPreUploadCheckDecision decision,
        string expectedDecisionPreview)
    {
        DesktopPreUploadCheckRequestPreview requestPreview =
            DesktopPreUploadCheckRequestPreview.TryCreate(
                DesktopUploadSelectedFile.VisualSmokeFile,
                FakeDesktopVideoHashService.VisualSmokeSha256Hex,
                new DesktopUploadBusinessObjectKey(DesktopUploadBusinessObjectKey.VisualSmokeValue))
            ?? throw new InvalidOperationException("Preview was not created.");
        var client = new FakeDesktopPreUploadCheckClient(decision);

        DesktopPreUploadCheckResult result = await client.CheckAsync(requestPreview, CancellationToken.None);

        Assert.Equal(decision, result.Decision);
        Assert.Equal(expectedDecisionPreview, result.DecisionPreview);
    }

    [Fact]
    public async Task HttpPreUploadCheckClientUsesExistingEndpointAndSafeRequestPayload()
    {
        string accessToken = CreateSensitiveValue("access");
        Guid userId = Guid.NewGuid();
        Guid deviceId = Guid.NewGuid();
        Guid groupNodeId = Guid.NewGuid();
        DesktopSessionState sessionState = await CreateSignedInSessionStateAsync(userId, deviceId, accessToken);
        var groupSelectionState = new DesktopGroupSelectionState();
        Assert.True(groupSelectionState.TrySelect(new DesktopGroupTreeNode(
            groupNodeId.ToString("D"),
            "Safe Group",
            Depth: 1,
            CanSelect: true)));
        DesktopPreUploadCheckRequestPreview requestPreview =
            DesktopPreUploadCheckRequestPreview.TryCreate(
                DesktopUploadSelectedFile.VisualSmokeFile,
                FakeDesktopVideoHashService.VisualSmokeSha256Hex,
                new DesktopUploadBusinessObjectKey(DesktopUploadBusinessObjectKey.VisualSmokeValue),
                groupSelectionState.SelectedGroup)
            ?? throw new InvalidOperationException("Preview was not created.");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new
            {
                preUploadCheckId = Guid.NewGuid(),
                decision = "ALLOW",
                canUploadToSite = true,
                reasonCode = "fast_path_clear",
                message = $"server-message-{CreateSensitiveValue("refresh")}",
                existingPreUploadCheckId = (string?)null,
                requiredNextSteps = new[] { "UPLOAD_TO_SITE_DIRECT", "SEND_UPLOAD_RECEIPT" },
                sitePlan = new
                {
                    provider = "Stub",
                    externalVideoId = "site-video-001",
                    storageKey = "videos/site-video-001.mp4",
                    requiredReceiptEndpoint = "/api/video/upload-receipt"
                },
                checkedAtUtc = DateTimeOffset.UtcNow
            })
        });
        var client = new HttpDesktopPreUploadCheckClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            sessionState,
            sessionState);

        DesktopPreUploadCheckResult result = await client.CheckAsync(requestPreview, CancellationToken.None);

        Assert.Equal(DesktopPreUploadCheckStatus.Allowed, result.Status);
        Assert.Equal(DesktopPreUploadCheckDecision.Allow, result.Decision);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("/api/video/pre-upload-check", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal(accessToken, handler.LastRequest.Headers.Authorization?.Parameter);

        JsonNode body = JsonNode.Parse(handler.LastRequestBody ?? string.Empty)
            ?? throw new InvalidOperationException("Request JSON was not captured.");
        Assert.Equal(userId, body["userId"]?.GetValue<Guid>());
        Assert.Equal(deviceId, body["deviceId"]?.GetValue<Guid>());
        Assert.Equal(groupNodeId, body["groupNodeId"]?.GetValue<Guid>());
        Assert.Equal(DesktopUploadBusinessObjectKey.VisualSmokeValue, body["businessObjectKey"]?.GetValue<string>());
        Assert.Equal(DesktopUploadSelectedFile.VisualSmokeFileName, body["fileName"]?.GetValue<string>());
        Assert.Equal(DesktopUploadSelectedFile.VisualSmokeFileSizeBytes, body["sizeBytes"]?.GetValue<long>());
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, body["byteSha256"]?.GetValue<string>());
        Assert.Equal(DesktopUploadSelectedFile.VisualSmokeContentType, body["contentType"]?.GetValue<string>());
        Assert.NotNull(body["capturedAtUtc"]);
        Assert.DoesNotContain(accessToken, handler.LastRequestBody ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ALLOW", true, DesktopPreUploadCheckStatus.Allowed, DesktopPreUploadCheckDecision.Allow, "ALLOW")]
    [InlineData("ALLOW_WITH_REVIEW", true, DesktopPreUploadCheckStatus.AllowedWithReview, DesktopPreUploadCheckDecision.AllowWithReview, "ALLOW_WITH_REVIEW")]
    [InlineData("BLOCK_HARD_DUPLICATE", false, DesktopPreUploadCheckStatus.Blocked, DesktopPreUploadCheckDecision.BlockHardDuplicate, "BLOCK_HARD_DUPLICATE")]
    [InlineData("BLOCK_POSSIBLE_FALSIFICATION", false, DesktopPreUploadCheckStatus.Blocked, DesktopPreUploadCheckDecision.BlockPossibleFalsification, "BLOCK_POSSIBLE_FALSIFICATION")]
    public async Task HttpPreUploadCheckClientMapsExistingResponseDecisionsSafely(
        string apiDecision,
        bool canUploadToSite,
        DesktopPreUploadCheckStatus expectedStatus,
        DesktopPreUploadCheckDecision expectedDecision,
        string expectedDecisionPreview)
    {
        DesktopSessionState sessionState = await CreateSignedInSessionStateAsync(
            Guid.NewGuid(),
            deviceId: null,
            CreateSensitiveValue("access"));
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new
            {
                preUploadCheckId = Guid.NewGuid(),
                decision = apiDecision,
                canUploadToSite,
                reasonCode = "mapped",
                message = "safe server message is intentionally not surfaced",
                existingPreUploadCheckId = (string?)null,
                requiredNextSteps = canUploadToSite
                    ? new[] { "UPLOAD_TO_SITE_DIRECT", "SEND_UPLOAD_RECEIPT" }
                    : new[] { "DO_NOT_UPLOAD", "SHOW_DECISION_TO_USER" },
                sitePlan = canUploadToSite
                    ? new
                    {
                        provider = "Stub",
                        externalVideoId = "site-video-001",
                        storageKey = "videos/site-video-001.mp4",
                        requiredReceiptEndpoint = "/api/video/upload-receipt"
                    }
                    : null,
                checkedAtUtc = DateTimeOffset.UtcNow
            })
        });
        var client = new HttpDesktopPreUploadCheckClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            sessionState,
            sessionState);

        DesktopPreUploadCheckResult result = await client.CheckAsync(CreateVisualSmokePreUploadPreview(), CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedDecision, result.Decision);
        Assert.Equal(expectedDecisionPreview, result.DecisionPreview);
        Assert.DoesNotContain("safe server message", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpPreUploadCheckClientHandlesUnavailableUnauthorizedFailedAndMalformedResponsesSafely()
    {
        string accessToken = CreateSensitiveValue("access");
        DesktopSessionState sessionState = await CreateSignedInSessionStateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            accessToken);
        DesktopPreUploadCheckRequestPreview requestPreview = CreateVisualSmokePreUploadPreview();

        var unauthorized = new HttpDesktopPreUploadCheckClient(
            CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)),
            sessionState,
            sessionState);
        DesktopPreUploadCheckResult unauthorizedResult =
            await unauthorized.CheckAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopPreUploadCheckStatus.Unauthorized, unauthorizedResult.Status);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckLiveUnauthorizedMessage, unauthorizedResult.Message);

        var unavailable = new HttpDesktopPreUploadCheckClient(
            new HttpClient(new StubHttpMessageHandler(_ => throw new HttpRequestException(
                $"transport failed {accessToken}")))
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            sessionState,
            sessionState);
        DesktopPreUploadCheckResult unavailableResult =
            await unavailable.CheckAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopPreUploadCheckStatus.Unavailable, unavailableResult.Status);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckLiveUnavailableMessage, unavailableResult.Message);

        string rawFailureBody = $"{{\"accessToken\":\"{CreateSensitiveValue("response")}\"}}";
        var failed = new HttpDesktopPreUploadCheckClient(
            CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(rawFailureBody, Encoding.UTF8, "application/json")
            }),
            sessionState,
            sessionState);
        DesktopPreUploadCheckResult failedResult =
            await failed.CheckAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopPreUploadCheckStatus.Failed, failedResult.Status);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckLiveFailedMessage, failedResult.Message);
        Assert.DoesNotContain(rawFailureBody, failedResult.ToString(), StringComparison.Ordinal);

        var malformed = new HttpDesktopPreUploadCheckClient(
            CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"decision\":\"ALLOW\",", Encoding.UTF8, "application/json")
            }),
            sessionState,
            sessionState);
        DesktopPreUploadCheckResult malformedResult =
            await malformed.CheckAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopPreUploadCheckStatus.Malformed, malformedResult.Status);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckLiveMalformedMessage, malformedResult.Message);

        foreach (DesktopPreUploadCheckResult result in
            new[] { unauthorizedResult, unavailableResult, failedResult, malformedResult })
        {
            Assert.Null(result.Decision);
            Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("accessToken", result.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refreshToken", result.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authorization", result.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("raw response", result.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task HttpPreUploadCheckClientWithoutSessionDoesNotSendRequest()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new { })
        });
        var client = new HttpDesktopPreUploadCheckClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken: null),
            new StaticDesktopSessionState(DesktopSessionSnapshot.SignedOut));

        DesktopPreUploadCheckResult result =
            await client.CheckAsync(CreateVisualSmokePreUploadPreview(), CancellationToken.None);

        Assert.Equal(DesktopPreUploadCheckStatus.Unauthorized, result.Status);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task HttpUploadReceiptClientUsesExistingEndpointAndSafeRequestPayload()
    {
        string accessToken = CreateSensitiveValue("access");
        Guid userId = Guid.NewGuid();
        Guid deviceId = Guid.NewGuid();
        Guid groupNodeId = Guid.NewGuid();
        Guid preUploadCheckId = Guid.NewGuid();
        Guid uploadReceiptId = Guid.NewGuid();
        DesktopSessionState sessionState = await CreateSignedInSessionStateAsync(userId, deviceId, accessToken);
        DesktopUploadReceiptRequestPreview requestPreview =
            CreateLiveUploadReceiptPreview(preUploadCheckId, groupNodeId);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new
            {
                uploadReceiptId,
                preUploadCheckId,
                status = "ACCEPTED",
                accepted = true,
                wasAlreadyAccepted = false,
                message = $"server-message-{CreateSensitiveValue("refresh")}",
                analysisJobStatus = "queued",
                receivedAtUtc = DateTimeOffset.UtcNow
            })
        });
        var client = new HttpDesktopUploadReceiptClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            sessionState,
            sessionState);

        DesktopUploadReceiptResult result = await client.CreateAsync(requestPreview, CancellationToken.None);

        Assert.Equal(DesktopUploadReceiptStatus.Accepted, result.Status);
        Assert.Equal("ACCEPTED", result.StatusPreview);
        Assert.Equal(uploadReceiptId.ToString("D"), result.ReceiptId);
        Assert.Null(result.ServerCorrelationId);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptAcceptedLiveMessage, result.Message);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("/api/video/upload-receipt", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal(accessToken, handler.LastRequest.Headers.Authorization?.Parameter);

        JsonNode body = JsonNode.Parse(handler.LastRequestBody ?? string.Empty)
            ?? throw new InvalidOperationException("Request JSON was not captured.");
        Assert.Equal(preUploadCheckId, body["preUploadCheckId"]?.GetValue<Guid>());
        Assert.Equal(userId, body["userId"]?.GetValue<Guid>());
        Assert.Equal(deviceId, body["deviceId"]?.GetValue<Guid>());
        Assert.Equal(groupNodeId, body["groupNodeId"]?.GetValue<Guid>());
        Assert.Equal("site-video-001", body["externalVideoId"]?.GetValue<string>());
        Assert.Equal("videos/site-video-001.mp4", body["storageKey"]?.GetValue<string>());
        Assert.Equal("SUCCESS", body["siteStatus"]?.GetValue<string>());
        Assert.Equal(DesktopUploadSelectedFile.VisualSmokeFileSizeBytes, body["sizeBytes"]?.GetValue<long>());
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, body["byteSha256"]?.GetValue<string>());
        Assert.Equal($"desktop-upload-receipt-{preUploadCheckId:N}", body["idempotencyKey"]?.GetValue<string>());
        Assert.NotNull(body["uploadedAtUtc"]);
        Assert.Null(body["fileName"]);
        Assert.Null(body["contentType"]);
        Assert.Null(body["businessObjectKey"]);
        Assert.DoesNotContain(accessToken, handler.LastRequestBody ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("server-message", result.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ACCEPTED", false, DesktopUploadReceiptStatus.Accepted, "ACCEPTED")]
    [InlineData("ALREADY_ACCEPTED", true, DesktopUploadReceiptStatus.AlreadyAccepted, "ALREADY_ACCEPTED")]
    public async Task HttpUploadReceiptClientMapsExistingResponseStatusesSafely(
        string apiStatus,
        bool wasAlreadyAccepted,
        DesktopUploadReceiptStatus expectedStatus,
        string expectedStatusPreview)
    {
        Guid preUploadCheckId = Guid.NewGuid();
        Guid uploadReceiptId = Guid.NewGuid();
        DesktopSessionState sessionState = await CreateSignedInSessionStateAsync(
            Guid.NewGuid(),
            deviceId: null,
            CreateSensitiveValue("access"));
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new
            {
                uploadReceiptId,
                preUploadCheckId,
                status = apiStatus,
                accepted = true,
                wasAlreadyAccepted,
                message = "safe server message is intentionally not surfaced",
                analysisJobStatus = "queued",
                receivedAtUtc = DateTimeOffset.UtcNow
            })
        });
        var client = new HttpDesktopUploadReceiptClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            sessionState,
            sessionState);

        DesktopUploadReceiptResult result =
            await client.CreateAsync(CreateLiveUploadReceiptPreview(preUploadCheckId), CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedStatusPreview, result.StatusPreview);
        Assert.Equal(uploadReceiptId.ToString("D"), result.ReceiptId);
        Assert.DoesNotContain("safe server message", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpUploadReceiptClientHandlesUnavailableUnauthorizedFailedAndMalformedResponsesSafely()
    {
        string accessToken = CreateSensitiveValue("access");
        DesktopSessionState sessionState = await CreateSignedInSessionStateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            accessToken);
        DesktopUploadReceiptRequestPreview requestPreview =
            CreateLiveUploadReceiptPreview(Guid.NewGuid());

        var unauthorized = new HttpDesktopUploadReceiptClient(
            CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)),
            sessionState,
            sessionState);
        DesktopUploadReceiptResult unauthorizedResult =
            await unauthorized.CreateAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopUploadReceiptStatus.Unauthorized, unauthorizedResult.Status);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptLiveUnauthorizedMessage, unauthorizedResult.Message);

        var unavailable = new HttpDesktopUploadReceiptClient(
            new HttpClient(new StubHttpMessageHandler(_ => throw new HttpRequestException(
                $"transport failed {accessToken}")))
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            sessionState,
            sessionState);
        DesktopUploadReceiptResult unavailableResult =
            await unavailable.CreateAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopUploadReceiptStatus.Unavailable, unavailableResult.Status);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptLiveUnavailableMessage, unavailableResult.Message);

        string rawFailureBody = $"{{\"accessToken\":\"{CreateSensitiveValue("response")}\"}}";
        var failed = new HttpDesktopUploadReceiptClient(
            CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(rawFailureBody, Encoding.UTF8, "application/json")
            }),
            sessionState,
            sessionState);
        DesktopUploadReceiptResult failedResult =
            await failed.CreateAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopUploadReceiptStatus.Failed, failedResult.Status);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptLiveFailedMessage, failedResult.Message);
        Assert.DoesNotContain(rawFailureBody, failedResult.ToString(), StringComparison.Ordinal);

        var malformed = new HttpDesktopUploadReceiptClient(
            CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ACCEPTED\",", Encoding.UTF8, "application/json")
            }),
            sessionState,
            sessionState);
        DesktopUploadReceiptResult malformedResult =
            await malformed.CreateAsync(requestPreview, CancellationToken.None);
        Assert.Equal(DesktopUploadReceiptStatus.Malformed, malformedResult.Status);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptLiveMalformedMessage, malformedResult.Message);

        foreach (DesktopUploadReceiptResult result in
            new[] { unauthorizedResult, unavailableResult, failedResult, malformedResult })
        {
            Assert.Null(result.StatusPreview);
            Assert.Null(result.ReceiptId);
            Assert.Null(result.ServerCorrelationId);
            Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("accessToken", result.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refreshToken", result.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authorization", result.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("raw response", result.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task HttpUploadReceiptClientWithoutSessionDoesNotSendRequest()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new { })
        });
        var client = new HttpDesktopUploadReceiptClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken: null),
            new StaticDesktopSessionState(DesktopSessionSnapshot.SignedOut));

        DesktopUploadReceiptResult result =
            await client.CreateAsync(CreateLiveUploadReceiptPreview(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(DesktopUploadReceiptStatus.Unauthorized, result.Status);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SiteUploadActionRequiresSelectedFileSha256BusinessObjectKeyAndAllowedPreUploadCheckDecision()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "false"));

        Assert.Null(viewModel.SiteUploadRequestPreview);
        Assert.False(viewModel.HasSiteUploadRequestPreview);
        Assert.False(viewModel.CanUploadToSite);

        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);
        viewModel.BusinessObjectKeyInput = "report-draft-001";
        _ = viewModel.ApplyBusinessObjectKey();

        Assert.Null(viewModel.SiteUploadRequestPreview);
        Assert.False(viewModel.CanUploadToSite);

        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(viewModel.SiteUploadRequestPreview);
        Assert.True(viewModel.HasSiteUploadRequestPreview);
        Assert.True(viewModel.CanUploadToSite);
        Assert.Equal(DesktopUploadSectionText.SiteUploadReadyMessage, viewModel.SiteUploadStatusMessage);
#else
        Assert.Null(viewModel.SiteUploadRequestPreview);
        Assert.False(viewModel.CanUploadToSite);
#endif
    }

    [Fact]
    public async Task SiteUploadRequestPreviewShowsOnlySafeFields()
    {
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(
                Path.Combine("private-folder", "nested", "safe-preview.mp4"),
                42,
                "video/mp4",
                DesktopVideoHashSource.VisualSmoke)),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "false"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, " report-draft-001 ");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        DesktopSiteUploadRequestPreview preview =
            viewModel.SiteUploadRequestPreview ?? throw new InvalidOperationException("Site upload preview was not created.");

        Assert.Equal("safe-preview.mp4", preview.FileName);
        Assert.Equal(42, preview.SizeBytes);
        Assert.Equal("video/mp4", preview.ContentType);
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, preview.Sha256Hex);
        Assert.Equal("report-draft-001", preview.BusinessObjectKeyPreview);
        Assert.Equal("ALLOW", preview.PreUploadCheckDecisionPreview);
        Assert.Equal(DesktopPreUploadCheckRequestPreview.CapturedAtUtcPlaceholder, preview.CapturedAtUtc);
#else
        Assert.Null(viewModel.SiteUploadRequestPreview);
#endif
    }

    [Fact]
    public async Task SiteUploadRequestPreviewDoesNotShowFullLocalPath()
    {
        string privateFolder = "operator-private-site-upload-source";
        string localPath = Path.Combine(Path.GetTempPath(), privateFolder, "safe-preview.mp4");
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(
                localPath,
                42,
                "video/mp4",
                DesktopVideoHashSource.VisualSmoke)),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "false"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        DesktopSiteUploadRequestPreview preview =
            viewModel.SiteUploadRequestPreview ?? throw new InvalidOperationException("Site upload preview was not created.");
        string visibleState = preview.ToString()
            + " "
            + viewModel.SiteUploadStatusMessage
            + " "
            + (viewModel.SiteUploadStatusPreview ?? string.Empty);

        Assert.Equal("safe-preview.mp4", preview.FileName);
        Assert.DoesNotContain(Path.GetTempPath(), visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(privateFolder, visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\", preview.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("/", preview.FileName, StringComparison.Ordinal);
#else
        Assert.Null(viewModel.SiteUploadRequestPreview);
#endif
    }

    [Fact]
    public async Task DisabledSiteUploadShowsSafeDeferredMessageAndDoesNotCallBoundary()
    {
        var siteUploadClient = new ThrowingDesktopDirectSiteUploadClient();
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            siteUploadClient,
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "false"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

        DesktopSiteUploadResult? result = await viewModel.UploadToSiteAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(result);
        Assert.Equal(DesktopSiteUploadStatus.Deferred, result.Status);
        Assert.Null(result.StatusPreview);
        Assert.Null(result.ExternalVideoId);
        Assert.Null(result.SiteStorageKey);
        Assert.Equal(DesktopUploadSectionText.SiteUploadDeferredMessage, viewModel.SiteUploadStatusMessage);
        Assert.Equal(0, siteUploadClient.CallCount);
#else
        Assert.Null(result);
        Assert.Equal(0, siteUploadClient.CallCount);
#endif
    }

    [Fact]
    public async Task EnabledSiteUploadShowsFakeSuccessResult()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new FakeDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "true"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

        DesktopSiteUploadResult? result = await viewModel.UploadToSiteAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(result);
        Assert.Equal(DesktopSiteUploadStatus.Succeeded, result.Status);
        Assert.Equal("SUCCESS", result.StatusPreview);
        Assert.Equal("visual-smoke-external-video-001", result.ExternalVideoId);
        Assert.Equal("visual-smoke/site/video-001", result.SiteStorageKey);
        Assert.True(viewModel.HasSiteUploadResult);
        Assert.Equal("SUCCESS", viewModel.SiteUploadStatusPreview);
        Assert.Equal("visual-smoke-external-video-001", viewModel.SiteUploadExternalVideoId);
        Assert.Equal("visual-smoke/site/video-001", viewModel.SiteUploadSiteStorageKey);
        Assert.Equal(DesktopUploadSectionText.SiteUploadSuccessDevMessage, viewModel.SiteUploadStatusMessage);
#else
        Assert.Null(result);
        Assert.False(viewModel.HasSiteUploadResult);
#endif
    }

    [Fact]
    public async Task UploadReceiptActionRequiresSelectedFileSha256BusinessObjectKeyAllowedPreUploadCheckAndSuccessfulSiteUpload()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new FakeDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "false"));

        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.HasUploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);

        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);
        viewModel.BusinessObjectKeyInput = "report-draft-001";
        _ = viewModel.ApplyBusinessObjectKey();
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(viewModel.SiteUploadRequestPreview);
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);

        _ = await viewModel.UploadToSiteAsync(CancellationToken.None);

        Assert.NotNull(viewModel.UploadReceiptRequestPreview);
        Assert.True(viewModel.HasUploadReceiptRequestPreview);
        Assert.True(viewModel.CanCreateUploadReceipt);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptReadyMessage, viewModel.UploadReceiptStatusMessage);
#else
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);
#endif
    }

    [Fact]
    public async Task UploadReceiptRequestPreviewShowsOnlySafeFields()
    {
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(
                Path.Combine("private-folder", "nested", "safe-preview.mp4"),
                42,
                "video/mp4",
                DesktopVideoHashSource.VisualSmoke)),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new DisabledDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "false"));

        _ = await PrepareSuccessfulSiteUploadAsync(viewModel, " report-draft-001 ");

#if DEBUG
        DesktopUploadReceiptRequestPreview preview =
            viewModel.UploadReceiptRequestPreview ?? throw new InvalidOperationException("UploadReceipt preview was not created.");

        Assert.Equal("safe-preview.mp4", preview.FileName);
        Assert.Equal(42, preview.SizeBytes);
        Assert.Equal("video/mp4", preview.ContentType);
        Assert.Equal(FakeDesktopVideoHashService.VisualSmokeSha256Hex, preview.Sha256Hex);
        Assert.Equal("report-draft-001", preview.BusinessObjectKeyPreview);
        Assert.Equal("ALLOW", preview.PreUploadCheckDecisionPreview);
        Assert.Equal("visual-smoke-external-video-001", preview.ExternalVideoId);
        Assert.Equal("visual-smoke/site/video-001", preview.SiteStorageKey);
        Assert.Equal("SUCCESS", preview.SiteUploadStatusPreview);
        Assert.Equal(DesktopPreUploadCheckRequestPreview.CapturedAtUtcPlaceholder, preview.CapturedAtUtc);
#else
        Assert.Null(viewModel.UploadReceiptRequestPreview);
#endif
    }

    [Fact]
    public async Task UploadReceiptRequestPreviewDoesNotShowFullLocalPath()
    {
        string privateFolder = "operator-private-upload-receipt-source";
        string localPath = Path.Combine(Path.GetTempPath(), privateFolder, "safe-preview.mp4");
        var viewModel = CreateViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected(
                localPath,
                42,
                "video/mp4",
                DesktopVideoHashSource.VisualSmoke)),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new DisabledDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "false"));

        _ = await PrepareSuccessfulSiteUploadAsync(viewModel, "report-draft-001");

#if DEBUG
        DesktopUploadReceiptRequestPreview preview =
            viewModel.UploadReceiptRequestPreview ?? throw new InvalidOperationException("UploadReceipt preview was not created.");
        string visibleState = preview.ToString()
            + " "
            + viewModel.UploadReceiptStatusMessage
            + " "
            + (viewModel.UploadReceiptStatusPreview ?? string.Empty);

        Assert.Equal("safe-preview.mp4", preview.FileName);
        Assert.DoesNotContain(Path.GetTempPath(), visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(privateFolder, visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\", preview.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("/", preview.FileName, StringComparison.Ordinal);
#else
        Assert.Null(viewModel.UploadReceiptRequestPreview);
#endif
    }

    [Fact]
    public async Task DisabledUploadReceiptShowsSafeDeferredMessageAndDoesNotCallBoundary()
    {
        var uploadReceiptClient = new ThrowingDesktopUploadReceiptClient();
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            uploadReceiptClient,
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "false"));

        _ = await PrepareSuccessfulSiteUploadAsync(viewModel, "report-draft-001");

        DesktopUploadReceiptResult? result = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(result);
        Assert.Equal(DesktopUploadReceiptStatus.Deferred, result.Status);
        Assert.Null(result.StatusPreview);
        Assert.Null(result.ReceiptId);
        Assert.Null(result.ServerCorrelationId);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptDeferredMessage, viewModel.UploadReceiptStatusMessage);
        Assert.Equal(0, uploadReceiptClient.CallCount);
#else
        Assert.Null(result);
        Assert.Equal(0, uploadReceiptClient.CallCount);
#endif
    }

    [Fact]
    public async Task EnabledUploadReceiptShowsFakeAcceptedResult()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new FakeDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "true"));

        _ = await PrepareSuccessfulSiteUploadAsync(viewModel, "report-draft-001");

        DesktopUploadReceiptResult? result = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(result);
        Assert.Equal(DesktopUploadReceiptStatus.Accepted, result.Status);
        Assert.Equal("ACCEPTED", result.StatusPreview);
        Assert.Equal("visual-smoke-upload-receipt-001", result.ReceiptId);
        Assert.Equal("visual-smoke-correlation-001", result.ServerCorrelationId);
        Assert.True(viewModel.HasUploadReceiptResult);
        Assert.Equal("ACCEPTED", viewModel.UploadReceiptStatusPreview);
        Assert.Equal("visual-smoke-upload-receipt-001", viewModel.UploadReceiptId);
        Assert.Equal("visual-smoke-correlation-001", viewModel.UploadReceiptServerCorrelationId);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptAcceptedDevMessage, viewModel.UploadReceiptStatusMessage);
#else
        Assert.Null(result);
        Assert.False(viewModel.HasUploadReceiptResult);
#endif
    }

    [Theory]
    [InlineData(DesktopPreUploadCheckDecision.Allow, true, "ALLOW")]
    [InlineData(DesktopPreUploadCheckDecision.AllowWithReview, true, "ALLOW_WITH_REVIEW")]
    [InlineData(DesktopPreUploadCheckDecision.BlockHardDuplicate, false, "BLOCK_HARD_DUPLICATE")]
    [InlineData(DesktopPreUploadCheckDecision.BlockPossibleFalsification, false, "BLOCK_POSSIBLE_FALSIFICATION")]
    public async Task UploadReceiptActionAllowsOnlyAllowedPreUploadCheckDecisionsAndSuccessfulSiteUpload(
        DesktopPreUploadCheckDecision decision,
        bool expectedCanCreateReceipt,
        string expectedDecisionPreview)
    {
        var uploadReceiptClient = new ThrowingDesktopUploadReceiptClient();
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(decision),
            new FakeDesktopDirectSiteUploadClient(),
            uploadReceiptClient,
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "true"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        Assert.Equal(expectedDecisionPreview, viewModel.PreUploadCheckDecisionPreview);

        if (expectedCanCreateReceipt)
        {
            _ = await viewModel.UploadToSiteAsync(CancellationToken.None);
            Assert.True(viewModel.HasSiteUploadResult);
            Assert.NotNull(viewModel.UploadReceiptRequestPreview);
            Assert.Equal(expectedDecisionPreview, viewModel.UploadReceiptRequestPreview.PreUploadCheckDecisionPreview);
            Assert.True(viewModel.CanCreateUploadReceipt);
            Assert.Equal(DesktopUploadSectionText.UploadReceiptReadyMessage, viewModel.UploadReceiptStatusMessage);
        }
        else
        {
            Assert.False(viewModel.CanUploadToSite);
            Assert.Null(viewModel.SiteUploadRequestPreview);
            Assert.Null(viewModel.UploadReceiptRequestPreview);
            Assert.False(viewModel.CanCreateUploadReceipt);
            DesktopUploadReceiptResult? result = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);
            Assert.Null(result);
            Assert.Equal(0, uploadReceiptClient.CallCount);
            Assert.Equal(DesktopUploadSectionText.UploadReceiptNotReadyMessage, viewModel.UploadReceiptStatusMessage);
        }
#else
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);
#endif
    }

    [Fact]
    public async Task MissingOrFailedSiteUploadDoesNotAllowUploadReceiptAction()
    {
        var uploadReceiptClient = new ThrowingDesktopUploadReceiptClient();
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new DisabledDesktopDirectSiteUploadClient(),
            uploadReceiptClient,
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "false", "true"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(viewModel.SiteUploadRequestPreview);
        Assert.False(viewModel.HasSiteUploadResult);
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);

        _ = await viewModel.UploadToSiteAsync(CancellationToken.None);

        Assert.False(viewModel.HasSiteUploadResult);
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);

        DesktopUploadReceiptResult? result = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);
        Assert.Null(result);
        Assert.Equal(0, uploadReceiptClient.CallCount);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptNotReadyMessage, viewModel.UploadReceiptStatusMessage);
#else
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.False(viewModel.CanCreateUploadReceipt);
#endif
    }

    [Theory]
    [InlineData(DesktopPreUploadCheckDecision.Allow, true, "ALLOW")]
    [InlineData(DesktopPreUploadCheckDecision.AllowWithReview, true, "ALLOW_WITH_REVIEW")]
    [InlineData(DesktopPreUploadCheckDecision.BlockHardDuplicate, false, "BLOCK_HARD_DUPLICATE")]
    [InlineData(DesktopPreUploadCheckDecision.BlockPossibleFalsification, false, "BLOCK_POSSIBLE_FALSIFICATION")]
    public async Task SiteUploadActionAllowsOnlyAllowedPreUploadCheckDecisions(
        DesktopPreUploadCheckDecision decision,
        bool expectedCanUpload,
        string expectedDecisionPreview)
    {
        var siteUploadClient = new ThrowingDesktopDirectSiteUploadClient();
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(decision),
            siteUploadClient,
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true"));

        _ = await PreparePreUploadCheckPreviewAsync(viewModel, "report-draft-001");
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);

#if DEBUG
        Assert.Equal(expectedDecisionPreview, viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal(expectedCanUpload, viewModel.CanUploadToSite);

        if (expectedCanUpload)
        {
            Assert.NotNull(viewModel.SiteUploadRequestPreview);
            Assert.Equal(expectedDecisionPreview, viewModel.SiteUploadRequestPreview.PreUploadCheckDecisionPreview);
            Assert.Equal(DesktopUploadSectionText.SiteUploadReadyMessage, viewModel.SiteUploadStatusMessage);
        }
        else
        {
            Assert.Null(viewModel.SiteUploadRequestPreview);
            Assert.False(viewModel.HasSiteUploadRequestPreview);
            Assert.Equal(
                DesktopUploadSectionText.SiteUploadBlockedByPreUploadCheckMessage,
                viewModel.SiteUploadStatusMessage);
            DesktopSiteUploadResult? result = await viewModel.UploadToSiteAsync(CancellationToken.None);
            Assert.Null(result);
            Assert.Equal(0, siteUploadClient.CallCount);
        }
#else
        Assert.Null(viewModel.SiteUploadRequestPreview);
        Assert.False(viewModel.CanUploadToSite);
#endif
    }

    [Fact]
    public async Task RealHashBoundaryComputesLowercaseSha256ForSelectedLocalFile()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string localFile = Path.Combine(tempDirectory, "safe-preview.mp4");

        try
        {
            await File.WriteAllTextAsync(localFile, "abc");
            var request = DesktopVideoHashRequest.FromSource(DesktopVideoHashSource.FromLocalFilePath(localFile));

            DesktopVideoHashResult result = await new DesktopVideoHashService().CalculateSha256Async(
                request,
                CancellationToken.None);

            Assert.Equal(DesktopVideoHashStatus.Succeeded, result.Status);
            Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", result.Sha256Hex);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task SafeHashErrorsDoNotExposeFullLocalPath()
    {
        string privateFolder = "operator-private-hash-source";
        string localFile = Path.Combine(Path.GetTempPath(), privateFolder, "missing-video.mp4");
        var viewModel = CreateViewModel(
            new StaticDesktopVideoFilePicker(DesktopVideoFilePickerResult.Selected(
                "safe-preview.mp4",
                42,
                "video/mp4",
                DesktopVideoHashSource.FromLocalFilePath(localFile))),
            new DesktopVideoHashService());

        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        DesktopVideoHashResult? result = await viewModel.CalculateSha256Async(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(DesktopVideoHashStatus.Unavailable, result.Status);
        Assert.Null(viewModel.Sha256Hex);
        Assert.Equal(DesktopUploadSectionText.HashUnavailableMessage, viewModel.HashStatusMessage);

        string visibleState = viewModel.HashStatusMessage + " " + result;
        Assert.DoesNotContain(Path.GetTempPath(), visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(privateFolder, visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("missing-video.mp4", visibleState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\", visibleState, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignOutResetReturnsUploadStateToWorkspace()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new FakeDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "true"));

        viewModel.OpenUploadSection();
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);
        viewModel.BusinessObjectKeyInput = "report-draft-001";
        _ = viewModel.ApplyBusinessObjectKey();
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);
        _ = await viewModel.UploadToSiteAsync(CancellationToken.None);
        _ = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(viewModel.SiteUploadRequestPreview);
        Assert.True(viewModel.HasSiteUploadResult);
        Assert.NotNull(viewModel.UploadReceiptRequestPreview);
        Assert.True(viewModel.HasUploadReceiptResult);
#endif

        viewModel.ResetForSignedOutState();

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
        Assert.Equal(DesktopUploadSectionText.HashNotReadyMessage, viewModel.HashStatusMessage);
        Assert.Null(viewModel.Sha256Hex);
        Assert.False(viewModel.CanCalculateHash);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.Equal(string.Empty, viewModel.BusinessObjectKeyInput);
        Assert.Equal(DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage, viewModel.BusinessObjectKeyStatusMessage);
        Assert.Null(viewModel.PreUploadCheckRequestPreview);
        Assert.Null(viewModel.PreUploadCheckResult);
        Assert.Null(viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckNotReadyMessage, viewModel.PreUploadCheckStatusMessage);
        Assert.Null(viewModel.SiteUploadRequestPreview);
        Assert.Null(viewModel.SiteUploadResult);
        Assert.Null(viewModel.SiteUploadStatusPreview);
        Assert.Null(viewModel.SiteUploadExternalVideoId);
        Assert.Null(viewModel.SiteUploadSiteStorageKey);
        Assert.Equal(DesktopUploadSectionText.SiteUploadNotReadyMessage, viewModel.SiteUploadStatusMessage);
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.Null(viewModel.UploadReceiptResult);
        Assert.Null(viewModel.UploadReceiptStatusPreview);
        Assert.Null(viewModel.UploadReceiptId);
        Assert.Null(viewModel.UploadReceiptServerCorrelationId);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptNotReadyMessage, viewModel.UploadReceiptStatusMessage);
    }

    [Fact]
    public async Task BackToWorkspaceResetsSelectedFileState()
    {
        var viewModel = CreateViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new FakeDesktopUploadReceiptClient(),
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "true"));

        viewModel.OpenUploadSection();
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);
        viewModel.BusinessObjectKeyInput = "report-draft-001";
        _ = viewModel.ApplyBusinessObjectKey();
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);
        _ = await viewModel.UploadToSiteAsync(CancellationToken.None);
        _ = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);

#if DEBUG
        Assert.NotNull(viewModel.SiteUploadRequestPreview);
        Assert.True(viewModel.HasSiteUploadResult);
        Assert.NotNull(viewModel.UploadReceiptRequestPreview);
        Assert.True(viewModel.HasUploadReceiptResult);
#endif

        viewModel.BackToWorkspace();

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
        Assert.Equal(DesktopUploadSectionText.HashNotReadyMessage, viewModel.HashStatusMessage);
        Assert.Null(viewModel.Sha256Hex);
        Assert.False(viewModel.CanCalculateHash);
        Assert.Null(viewModel.BusinessObjectKey);
        Assert.Null(viewModel.BusinessObjectKeyPreview);
        Assert.Equal(string.Empty, viewModel.BusinessObjectKeyInput);
        Assert.Equal(DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage, viewModel.BusinessObjectKeyStatusMessage);
        Assert.Null(viewModel.PreUploadCheckRequestPreview);
        Assert.Null(viewModel.PreUploadCheckResult);
        Assert.Null(viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal(DesktopUploadSectionText.PreUploadCheckNotReadyMessage, viewModel.PreUploadCheckStatusMessage);
        Assert.Null(viewModel.SiteUploadRequestPreview);
        Assert.Null(viewModel.SiteUploadResult);
        Assert.Null(viewModel.SiteUploadStatusPreview);
        Assert.Null(viewModel.SiteUploadExternalVideoId);
        Assert.Null(viewModel.SiteUploadSiteStorageKey);
        Assert.Equal(DesktopUploadSectionText.SiteUploadNotReadyMessage, viewModel.SiteUploadStatusMessage);
        Assert.Null(viewModel.UploadReceiptRequestPreview);
        Assert.Null(viewModel.UploadReceiptResult);
        Assert.Null(viewModel.UploadReceiptStatusPreview);
        Assert.Null(viewModel.UploadReceiptId);
        Assert.Null(viewModel.UploadReceiptServerCorrelationId);
        Assert.Equal(DesktopUploadSectionText.UploadReceiptNotReadyMessage, viewModel.UploadReceiptStatusMessage);
    }

    [Fact]
    public void UploadVisualStringsDoNotExposeTokensPasswordAuthorizationOrRawSession()
    {
        string[] visibleStrings =
        [
            DesktopUploadSectionText.Title,
            DesktopUploadSectionText.Description,
            DesktopUploadSectionText.NavigationCardMessage,
            DesktopUploadSectionText.GroupContextTitle,
            DesktopUploadSectionText.GroupContextMissingMessage,
            DesktopUploadSectionText.GroupContextNameLabel,
            DesktopUploadSectionText.GroupContextIdLabel,
            DesktopUploadSectionText.StepOneTitle,
            DesktopUploadSectionText.SelectVideoFileButton,
            DesktopUploadSectionText.PlaceholderResult,
            DesktopUploadSectionText.SelectingFileMessage,
            DesktopUploadSectionText.SelectionCanceledMessage,
            DesktopUploadSectionText.SelectedFilePreviewMessage,
            DesktopUploadSectionText.SelectionUnavailableMessage,
            DesktopUploadSectionText.BackToWorkspaceButton,
            DesktopUploadSectionText.SelectedFileNameLabel,
            DesktopUploadSectionText.SelectedFileSizeLabel,
            DesktopUploadSectionText.SelectedFileContentTypeLabel,
            DesktopUploadSectionText.UnknownContentTypeValue,
            DesktopUploadSectionText.StepTwoTitle,
            DesktopUploadSectionText.CalculateSha256Button,
            DesktopUploadSectionText.CalculateSha256BusyButton,
            DesktopUploadSectionText.HashNotReadyMessage,
            DesktopUploadSectionText.HashReadyMessage,
            DesktopUploadSectionText.HashInProgressMessage,
            DesktopUploadSectionText.HashSucceededMessage,
            DesktopUploadSectionText.HashUnavailableMessage,
            DesktopUploadSectionText.HashCanceledMessage,
            DesktopUploadSectionText.Sha256ResultLabel,
            DesktopUploadSectionText.StepThreeTitle,
            DesktopUploadSectionText.BusinessObjectKeyLabel,
            DesktopUploadSectionText.BusinessObjectKeyHint,
            DesktopUploadSectionText.BusinessObjectKeyApplyButton,
            DesktopUploadSectionText.BusinessObjectKeyUseFakeButton,
            DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage,
            DesktopUploadSectionText.BusinessObjectKeyControlCharacterValidationMessage,
            DesktopUploadSectionText.BusinessObjectKeyLengthValidationMessage,
            DesktopUploadSectionText.BusinessObjectKeySecretValidationMessage,
            DesktopUploadSectionText.BusinessObjectKeyAcceptedMessage,
            DesktopUploadSectionText.BusinessObjectKeyPreviewLabel,
            DesktopUploadSectionText.StepFourTitle,
            DesktopUploadSectionText.PreUploadCheckNotReadyMessage,
            DesktopUploadSectionText.PreUploadCheckReadyMessage,
            DesktopUploadSectionText.PreUploadCheckLiveReadyMessage,
            DesktopUploadSectionText.PreUploadCheckInProgressMessage,
            DesktopUploadSectionText.PreUploadCheckLiveInProgressMessage,
            DesktopUploadSectionText.PreUploadCheckDeferredMessage,
            DesktopUploadSectionText.PreUploadCheckAllowedDevMessage,
            DesktopUploadSectionText.PreUploadCheckAllowWithReviewDevMessage,
            DesktopUploadSectionText.PreUploadCheckBlockHardDuplicateDevMessage,
            DesktopUploadSectionText.PreUploadCheckBlockPossibleFalsificationDevMessage,
            DesktopUploadSectionText.PreUploadCheckAllowedLiveMessage,
            DesktopUploadSectionText.PreUploadCheckAllowWithReviewLiveMessage,
            DesktopUploadSectionText.PreUploadCheckBlockHardDuplicateLiveMessage,
            DesktopUploadSectionText.PreUploadCheckBlockPossibleFalsificationLiveMessage,
            DesktopUploadSectionText.PreUploadCheckLiveUnauthorizedMessage,
            DesktopUploadSectionText.PreUploadCheckLiveUnavailableMessage,
            DesktopUploadSectionText.PreUploadCheckLiveFailedMessage,
            DesktopUploadSectionText.PreUploadCheckLiveMalformedMessage,
            DesktopUploadSectionText.PreUploadCheckCanceledMessage,
            DesktopUploadSectionText.PreUploadCheckButton,
            DesktopUploadSectionText.PreUploadCheckBusyButton,
            DesktopUploadSectionText.PreUploadCheckFileNameLabel,
            DesktopUploadSectionText.PreUploadCheckFileSizeLabel,
            DesktopUploadSectionText.PreUploadCheckContentTypeLabel,
            DesktopUploadSectionText.PreUploadCheckSha256Label,
            DesktopUploadSectionText.PreUploadCheckBusinessObjectKeyLabel,
            DesktopUploadSectionText.PreUploadCheckCapturedAtUtcLabel,
            DesktopUploadSectionText.PreUploadCheckDecisionLabel,
            DesktopUploadSectionText.StepFiveTitle,
            DesktopUploadSectionText.SiteUploadNotReadyMessage,
            DesktopUploadSectionText.SiteUploadReadyMessage,
            DesktopUploadSectionText.SiteUploadBlockedByPreUploadCheckMessage,
            DesktopUploadSectionText.SiteUploadInProgressMessage,
            DesktopUploadSectionText.SiteUploadDeferredMessage,
            DesktopUploadSectionText.SiteUploadSuccessDevMessage,
            DesktopUploadSectionText.SiteUploadCanceledMessage,
            DesktopUploadSectionText.SiteUploadButton,
            DesktopUploadSectionText.SiteUploadBusyButton,
            DesktopUploadSectionText.SiteUploadFileNameLabel,
            DesktopUploadSectionText.SiteUploadFileSizeLabel,
            DesktopUploadSectionText.SiteUploadContentTypeLabel,
            DesktopUploadSectionText.SiteUploadSha256Label,
            DesktopUploadSectionText.SiteUploadBusinessObjectKeyLabel,
            DesktopUploadSectionText.SiteUploadPreUploadCheckDecisionLabel,
            DesktopUploadSectionText.SiteUploadCapturedAtUtcLabel,
            DesktopUploadSectionText.SiteUploadResultStatusLabel,
            DesktopUploadSectionText.SiteUploadExternalVideoIdLabel,
            DesktopUploadSectionText.SiteUploadSiteStorageKeyLabel,
            DesktopUploadSectionText.StepSixTitle,
            DesktopUploadSectionText.UploadReceiptNotReadyMessage,
            DesktopUploadSectionText.UploadReceiptReadyMessage,
            DesktopUploadSectionText.UploadReceiptLiveReadyMessage,
            DesktopUploadSectionText.UploadReceiptInProgressMessage,
            DesktopUploadSectionText.UploadReceiptLiveInProgressMessage,
            DesktopUploadSectionText.UploadReceiptDeferredMessage,
            DesktopUploadSectionText.UploadReceiptAcceptedDevMessage,
            DesktopUploadSectionText.UploadReceiptAcceptedLiveMessage,
            DesktopUploadSectionText.UploadReceiptAlreadyAcceptedLiveMessage,
            DesktopUploadSectionText.UploadReceiptLiveUnauthorizedMessage,
            DesktopUploadSectionText.UploadReceiptLiveUnavailableMessage,
            DesktopUploadSectionText.UploadReceiptLiveFailedMessage,
            DesktopUploadSectionText.UploadReceiptLiveMalformedMessage,
            DesktopUploadSectionText.UploadReceiptCanceledMessage,
            DesktopUploadSectionText.UploadReceiptButton,
            DesktopUploadSectionText.UploadReceiptBusyButton,
            DesktopUploadSectionText.UploadReceiptFileNameLabel,
            DesktopUploadSectionText.UploadReceiptFileSizeLabel,
            DesktopUploadSectionText.UploadReceiptContentTypeLabel,
            DesktopUploadSectionText.UploadReceiptSha256Label,
            DesktopUploadSectionText.UploadReceiptBusinessObjectKeyLabel,
            DesktopUploadSectionText.UploadReceiptPreUploadCheckDecisionLabel,
            DesktopUploadSectionText.UploadReceiptExternalVideoIdLabel,
            DesktopUploadSectionText.UploadReceiptSiteStorageKeyLabel,
            DesktopUploadSectionText.UploadReceiptSiteUploadStatusLabel,
            DesktopUploadSectionText.UploadReceiptCapturedAtUtcLabel,
            DesktopUploadSectionText.UploadReceiptResultStatusLabel,
            DesktopUploadSectionText.UploadReceiptResultReceiptIdLabel,
            DesktopUploadSectionText.UploadReceiptResultServerCorrelationIdLabel,
            DesktopUploadSectionText.NextStepDeferredMessage,
            DesktopUploadSelectedFile.VisualSmokeFileName,
            DesktopUploadSelectedFile.VisualSmokeContentType,
            FakeDesktopVideoHashService.VisualSmokeSha256Hex,
            DesktopUploadBusinessObjectKey.VisualSmokeValue,
            DesktopPreUploadCheckRequestPreview.CapturedAtUtcPlaceholder,
            DesktopPreUploadCheckResult.FromFakeDecision(DesktopPreUploadCheckDecision.Allow).DecisionPreview ?? string.Empty,
            DesktopPreUploadCheckResult.FromFakeDecision(DesktopPreUploadCheckDecision.AllowWithReview).DecisionPreview ?? string.Empty,
            DesktopPreUploadCheckResult.FromFakeDecision(DesktopPreUploadCheckDecision.BlockHardDuplicate).DecisionPreview ?? string.Empty,
            DesktopPreUploadCheckResult.FromFakeDecision(DesktopPreUploadCheckDecision.BlockPossibleFalsification).DecisionPreview ?? string.Empty,
            DesktopSiteUploadResult.FakeSuccess.StatusPreview ?? string.Empty,
            DesktopSiteUploadResult.FakeSuccess.ExternalVideoId ?? string.Empty,
            DesktopSiteUploadResult.FakeSuccess.SiteStorageKey ?? string.Empty,
            DesktopUploadReceiptResult.FakeAccepted.StatusPreview ?? string.Empty,
            DesktopUploadReceiptResult.FakeAccepted.ReceiptId ?? string.Empty,
            DesktopUploadReceiptResult.FakeAccepted.ServerCorrelationId ?? string.Empty
        ];

        foreach (string visibleString in visibleStrings)
        {
            Assert.DoesNotContain("password", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authorization", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refreshToken", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("session_id", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("raw response", visibleString, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static DesktopUploadSectionViewModel CreateViewModel(IDesktopVideoFilePicker videoFilePicker)
    {
        return CreateViewModel(videoFilePicker, new FakeDesktopVideoHashService());
    }

    private static DesktopUploadSectionViewModel CreateViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService)
    {
        return CreateViewModel(videoFilePicker, videoHashService, DesktopUploadSectionOptions.Disabled);
    }

    private static DesktopUploadSectionViewModel CreateViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        DesktopUploadSectionOptions uploadSectionOptions)
    {
        return CreateViewModel(
            videoFilePicker,
            videoHashService,
            new DisabledDesktopPreUploadCheckClient(),
            uploadSectionOptions);
    }

    private static DesktopUploadSectionViewModel CreateViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        IDesktopPreUploadCheckClient preUploadCheckClient,
        DesktopUploadSectionOptions uploadSectionOptions)
    {
        return CreateViewModel(
            videoFilePicker,
            videoHashService,
            preUploadCheckClient,
            new DisabledDesktopDirectSiteUploadClient(),
            uploadSectionOptions);
    }

    private static DesktopUploadSectionViewModel CreateViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        IDesktopPreUploadCheckClient preUploadCheckClient,
        IDesktopDirectSiteUploadClient directSiteUploadClient,
        DesktopUploadSectionOptions uploadSectionOptions)
    {
        return CreateViewModel(
            videoFilePicker,
            videoHashService,
            preUploadCheckClient,
            directSiteUploadClient,
            new DisabledDesktopUploadReceiptClient(),
            uploadSectionOptions);
    }

    private static DesktopUploadSectionViewModel CreateViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        IDesktopPreUploadCheckClient preUploadCheckClient,
        IDesktopDirectSiteUploadClient directSiteUploadClient,
        IDesktopUploadReceiptClient uploadReceiptClient,
        DesktopUploadSectionOptions uploadSectionOptions)
    {
        return new DesktopUploadSectionViewModel(
            videoFilePicker,
            videoHashService,
            preUploadCheckClient,
            directSiteUploadClient,
            uploadReceiptClient,
            uploadSectionOptions);
    }

    private static async Task<DesktopPreUploadCheckRequestPreview> PreparePreUploadCheckPreviewAsync(
        DesktopUploadSectionViewModel viewModel,
        string businessObjectKey)
    {
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);
        viewModel.BusinessObjectKeyInput = businessObjectKey;
        _ = viewModel.ApplyBusinessObjectKey();

        return viewModel.PreUploadCheckRequestPreview
            ?? throw new InvalidOperationException("PreUploadCheck preview was not created.");
    }

    private static async Task<DesktopSiteUploadResult?> PrepareSuccessfulSiteUploadAsync(
        DesktopUploadSectionViewModel viewModel,
        string businessObjectKey)
    {
        _ = await PreparePreUploadCheckPreviewAsync(viewModel, businessObjectKey);
        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);
        return await viewModel.UploadToSiteAsync(CancellationToken.None);
    }

    private static DesktopPreUploadCheckRequestPreview CreateVisualSmokePreUploadPreview()
    {
        return DesktopPreUploadCheckRequestPreview.TryCreate(
            DesktopUploadSelectedFile.VisualSmokeFile,
            FakeDesktopVideoHashService.VisualSmokeSha256Hex,
            new DesktopUploadBusinessObjectKey(DesktopUploadBusinessObjectKey.VisualSmokeValue))
            ?? throw new InvalidOperationException("Preview was not created.");
    }

    private static DesktopUploadReceiptRequestPreview CreateLiveUploadReceiptPreview(
        Guid preUploadCheckId,
        Guid? groupNodeId = null)
    {
        DesktopSelectedGroupContext? selectedGroup = null;
        if (groupNodeId.HasValue)
        {
            var groupSelectionState = new DesktopGroupSelectionState();
            Assert.True(groupSelectionState.TrySelect(new DesktopGroupTreeNode(
                groupNodeId.Value.ToString("D"),
                "Safe Group",
                Depth: 1,
                CanSelect: true)));
            selectedGroup = groupSelectionState.SelectedGroup;
        }

        DesktopPreUploadCheckRequestPreview preUploadPreview =
            DesktopPreUploadCheckRequestPreview.TryCreate(
                DesktopUploadSelectedFile.VisualSmokeFile,
                FakeDesktopVideoHashService.VisualSmokeSha256Hex,
                new DesktopUploadBusinessObjectKey(DesktopUploadBusinessObjectKey.VisualSmokeValue),
                selectedGroup)
            ?? throw new InvalidOperationException("PreUploadCheck preview was not created.");
        DesktopPreUploadCheckResult preUploadResult = DesktopPreUploadCheckResult.FromLiveDecision(
            DesktopPreUploadCheckDecision.Allow,
            preUploadCheckId,
            "site-video-001",
            "videos/site-video-001.mp4");
        DesktopSiteUploadRequestPreview siteUploadPreview =
            DesktopSiteUploadRequestPreview.TryCreate(preUploadPreview, preUploadResult)
            ?? throw new InvalidOperationException("SiteUpload preview was not created.");
        DesktopSiteUploadResult siteUploadResult = DesktopSiteUploadResult.FromFakeSuccess(siteUploadPreview);

        return DesktopUploadReceiptRequestPreview.TryCreate(siteUploadPreview, siteUploadResult)
            ?? throw new InvalidOperationException("UploadReceipt preview was not created.");
    }

    private static async Task<DesktopSessionState> CreateSignedInSessionStateAsync(
        Guid userId,
        Guid? deviceId,
        string accessToken)
    {
        var sessionState = new DesktopSessionState(new DisabledDesktopSessionStore());
        DateTimeOffset issuedAtUtc = DateTimeOffset.UtcNow;

        await sessionState.SetSignedInAsync(
            DesktopAuthenticatedSession.Create(
                Guid.NewGuid(),
                userId,
                deviceId,
                "Live User",
                accessToken,
                refreshToken: null,
                issuedAtUtc,
                issuedAtUtc.AddMinutes(15),
                isOfflineRestricted: false),
            CancellationToken.None);

        return sessionState;
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("https://control-plane.local")
        };
    }

    private static StringContent JsonContent<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value),
            Encoding.UTF8,
            "application/json");
    }

    private static string CreateSensitiveValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private sealed class CancelingDesktopVideoFilePicker : IDesktopVideoFilePicker
    {
        public ValueTask<DesktopVideoFilePickerResult> PickVideoFileAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(DesktopVideoFilePickerResult.Canceled);
        }
    }

    private sealed class StaticDesktopVideoFilePicker(DesktopVideoFilePickerResult result) : IDesktopVideoFilePicker
    {
        public ValueTask<DesktopVideoFilePickerResult> PickVideoFileAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowingDesktopPreUploadCheckClient : IDesktopPreUploadCheckClient
    {
        public int CallCount { get; private set; }

        public ValueTask<DesktopPreUploadCheckResult> CheckAsync(
            DesktopPreUploadCheckRequestPreview requestPreview,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("Disabled fake PreUploadCheck boundary should not be called.");
        }
    }

    private sealed class ThrowingDesktopDirectSiteUploadClient : IDesktopDirectSiteUploadClient
    {
        public int CallCount { get; private set; }

        public ValueTask<DesktopSiteUploadResult> UploadAsync(
            DesktopSiteUploadRequestPreview requestPreview,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("Disabled fake site upload boundary should not be called.");
        }
    }

    private sealed class ThrowingDesktopUploadReceiptClient : IDesktopUploadReceiptClient
    {
        public int CallCount { get; private set; }

        public ValueTask<DesktopUploadReceiptResult> CreateAsync(
            DesktopUploadReceiptRequestPreview requestPreview,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("Disabled fake UploadReceipt boundary should not be called.");
        }
    }

    private sealed class StaticDesktopControlPlaneAccessTokenProvider(string? accessToken) :
        IDesktopControlPlaneAccessTokenProvider
    {
        public ValueTask<DesktopControlPlaneAccessTokenSnapshot> GetCurrentAccessTokenAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new DesktopControlPlaneAccessTokenSnapshot(
                IsAuthenticated: !string.IsNullOrWhiteSpace(accessToken),
                accessToken));
        }
    }

    private sealed class StaticDesktopSessionState(DesktopSessionSnapshot current) : IDesktopSessionState
    {
        public DesktopSessionSnapshot Current { get; private set; } = current;

        public ValueTask<DesktopSessionSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(Current);
        }

        public ValueTask<DesktopSessionSnapshot> SetSignedInAsync(
            DesktopAuthenticatedSession session,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Current = DesktopSessionSnapshot.FromSession(session);
            return ValueTask.FromResult(Current);
        }

        public ValueTask<DesktopSessionSnapshot> SignOutAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Current = DesktopSessionSnapshot.SignedOut;
            return ValueTask.FromResult(Current);
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) :
        HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responseFactory(request);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AnalyticsAutomation-Core.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}