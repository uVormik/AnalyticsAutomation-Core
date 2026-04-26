namespace App.Mobile.Android.Localization;

internal static class MobileUiText
{
    public const string ApplicationTitle = "Мобильный клиент";
    public const string NavigationSubtitle = "Локальная оболочка Android";
    public const string ShellBannerLabel = "Локальный режим";

    public const string MenuReports = "Полеты";
    public const string MenuHome = "Главная";
    public const string MenuUpload = "Загрузка";
    public const string MenuQueue = "Очередь";

    public const string HomeTitle = "Главная";
    public const string HomeIntro =
        "Текущая Android-оболочка работает локально и показывает только базовую мобильную навигацию без backend-интеграции.";

    public const string ReportsTitle = "Полеты";
    public const string ReportsIntro =
        "Это локальный report-first baseline: сначала создается черновик FPV-отчета, а видео и другие файлы живут как вложения к этому черновику.";
    public const string ReportsFeatureDisabledText =
        "Локальная оболочка черновиков отчетов сейчас скрыта флагом функции.";
    public const string ReportsLoadingText = "Загружается локальный список черновиков...";
    public const string ReportsCreateFpvDraftButton = "Создать отчет FPV";
    public const string ReportsEmptyTitle = "Черновиков пока нет";
    public const string ReportsEmptyMessage =
        "Создайте локальный FPV-черновик, чтобы проверить report-first оболочку без backend-сохранения, sync и upload.";
    public const string ReportDraftCardCreatedAtLabel = "Создан";
    public const string ReportDraftCardUpdatedAtLabel = "Обновлен";
    public const string ReportDraftCardStatusLabel = "Статус";
    public const string ReportDraftCardAttachmentCountLabel = "Вложений";

    public const string ReportDraftPageTitle = "Черновик FPV-отчета";
    public const string ReportDraftLocalOnlyNote =
        "Это локальный черновик отчета на устройстве. Он еще не сохранен в backend и не имеет server id.";
    public const string ReportDraftNoBackendSaveNote =
        "В этом срезе нет create-report API, businessObjectKey, PreUploadCheck, UploadReceipt, sync и upload. Это только локальная report-first оболочка.";
    public const string ReportDraftNotFoundTitle = "Черновик не найден";
    public const string ReportDraftNotFoundMessage =
        "Запрошенный локальный черновик отчета не найден в текущем in-memory store.";
    public const string ReportDraftAttachCurrentVideoButton = "Прикрепить текущее видео";
    public const string ReportDraftNoCurrentSelectedVideoText =
        "Сначала выберите или запишите видео в разделе «Медиа-вложения отчета», чтобы прикрепить его к текущему локальному черновику.";
    public const string ReportDraftNoAttachmentsText =
        "Локальных вложений пока нет. Выберите или запишите видео в этом разделе, чтобы прикрепить его как метаданные вложения без upload и без backend-сохранения.";
    public const string ReportDraftInvalidVideoSelectionText =
        "Не удалось обработать текущее локальное видео. Повторите выбор или запись без backend-вызовов.";
    public const string ReportDraftDuplicateAttachmentWarningText =
        "Это видео уже прикреплено к текущему локальному черновику отчета. Дубликат локально заблокирован.";
    public const string ReportDraftQueueButton = "Поставить отчет в локальную очередь";
    public const string ReportDraftQueueLocalOnlyNoteText =
        "Это локальная очередь черновика. Backend create-report и businessObjectKey пока не подключены.";
    public const string ReportDraftQueueInvalidDraftText =
        "Не удалось поместить отчет в локальную очередь: черновик не найден.";
    public const string ReportDraftQueueRequiresVideoText =
        "В этом прототипе локальная очередь черновика требует хотя бы одно видео-вложение.";
    public const string ReportDraftAlreadyQueuedText =
        "Этот локальный черновик уже находится в очереди устройства.";
    public const string ReportSectionBasicDataTitle = "Основные данные";
    public const string ReportSectionTargetAndResultTitle = "Цель и результат";
    public const string ReportSectionFrequenciesAndParametersTitle = "Частоты и параметры";
    public const string ReportSectionAttachmentsTitle = "Медиа-вложения";
    public const string ReportAttachmentKindLabel = "Тип вложения";
    public const string ReportAttachmentFileNameLabel = "Имя файла";
    public const string ReportAttachmentContentTypeLabel = "MIME type";
    public const string ReportAttachmentSourceLabel = "Источник";
    public const string ReportAttachmentAddedAtLabel = "Добавлено";
    public const string ReportAttachmentLocalAccessLabel = "Локальный доступ";
    public const string ReportAttachmentLocalAccessAvailableText =
        "Локальный доступ к файлу есть в текущем запуске.";
    public const string ReportAttachmentLocalAccessMissingText =
        "Доступны только локальные метаданные без живого доступа к файлу.";

    public const string ReportFieldDeviceTypeLabel = "Тип дрона";
    public const string ReportFieldSerialNumberLabel = "Серийный номер";
    public const string ReportFieldDeliveryStartLabel = "Начало доставки";
    public const string ReportFieldDeliveryTimeLabel = "Время доставки";
    public const string ReportFieldDistanceLabel = "Дистанция";
    public const string ReportFieldTargetTypeLabel = "Тип цели";
    public const string ReportFieldReasonLabel = "Причина";
    public const string ReportFieldCommentLabel = "Комментарий";
    public const string ReportFieldRadioFrequencyLabel = "Радиочастота";
    public const string ReportFieldVideoFrequencyLabel = "Видеочастота";
    public const string ReportFieldTestFlightLabel = "Тестовый полет";
    public const string ReportFieldTechnicalIssueTypeLabel = "Тип технической неисправности";
    public const string ReportFieldStatusLabel = "Статус";
    public const string ReportFieldWarheadTypeLabel = "Тип боевой части";
    public const string ReportFieldDetonatorLabel = "Детонатор";
    public const string ReportFieldNsuLabel = "НСУ";
    public const string ReportFieldPlaceholderText =
        "Поле пока работает как локальная заглушка без финального backend-контракта и без справочника значений.";
    public const string ReportDraftEditFieldButton = "Изменить поле";
    public const string ReportDraftSaveFieldValueButton = "Сохранить значение";
    public const string ReportDraftCancelFieldEditButton = "Отмена";
    public const string ReportDraftOpenSelectorButton = "Выбрать значение";
    public const string ReportDraftSelectorSheetTitle = "Локальный выбор значения";
    public const string ReportDraftSelectorSearchPlaceholder = "Поиск по локальным значениям-заглушкам";
    public const string ReportDraftSelectorApplyButton = "Применить";
    public const string ReportDraftSelectorClearButton = "Очистить";
    public const string ReportDraftSelectorEmptyState = "Подходящих локальных значений-заглушек не найдено.";
    public const string ReportDraftStubCatalogWarning =
        "Справочник-заглушка. Реальные значения будут загружаться с сервера.";
    public const string ReportDraftTextValuePlaceholder = "Введите локальное текстовое значение";
    public const string ReportDraftNumericValuePlaceholder = "Введите локальное числовое значение";
    public const string ReportDraftDateTimeValuePlaceholder = "Выберите дату и время локально";
    public const string ReportDraftToggleYesText = "Да";
    public const string ReportDraftToggleNoText = "Нет";
    public const string ReportDraftFieldRequiredMark = "*";
    public const string ReportDraftFieldNotFoundMessage =
        "Не удалось локально обновить поле: оно не найдено в текущем черновике.";
    public const string ReportDraftFieldUpdateFailedMessage =
        "Не удалось локально обновить значение поля.";

    public const string UploadTitle = "Загрузка";
    public const string UploadIntro =
        "На этом срезе доступны локальный выбор и запись видео на устройстве, а также локальная очередь-черновик. " +
        "Файлы не копируются, не сохраняются отдельно и не отправляются в backend.";
    public const string UploadCapabilityCardTitle = "Локальные media-возможности устройства";
    public const string UploadCapabilityCardSummary =
        "Текущий Android baseline открывает системный выбор видео и запись видео на устройстве, если камера поддерживается. " +
        "Это только local device media без upload и sync.";
    public const string UploadLocalDeviceBadge = "Только на устройстве";
    public const string UploadFilePickerLabel = "Файловый seam";
    public const string UploadGalleryVideoLabel = "Выбор видео из галереи";
    public const string UploadCameraCaptureLabel = "Запись видео с камеры";
    public const string UploadSelectVideoButton = "Выбрать видео";
    public const string UploadCaptureVideoButton = "Записать видео";
    public const string UploadLoadingCapabilitySnapshot = "Загружается локальная информация о media-возможностях...";
    public const string UploadNativePickerTitle = "Выберите одно видео";
    public const string UploadNativeCaptureTitle = "Запишите одно видео";
    public const string UploadSelectedVideoFallbackName = "без имени";
    public const string UploadNativePickerCancelledText =
        "Выбор видео отменен. Никакие файлы не были сохранены или отправлены.";
    public const string UploadNativePickerUnavailableText =
        "Не удалось открыть системный выбор видео на устройстве. Проверьте разрешения и повторите попытку.";
    public const string UploadNativePickerFailedText =
        "Не удалось завершить локальный выбор видео. Это только Android media baseline без upload и sync.";
    public const string UploadNativeCaptureCancelledText =
        "Запись видео отменена. Никакие файлы не были сохранены или отправлены.";
    public const string UploadNativeCaptureUnavailableText =
        "Системная запись видео недоступна на этом устройстве или сейчас не поддерживается.";
    public const string UploadNativeCapturePermissionDeniedText =
        "Доступ к камере не предоставлен. Разрешите использование камеры и повторите попытку.";
    public const string UploadNativeCaptureFailedText =
        "Не удалось завершить локальную запись видео. Это только Android media baseline без upload и sync.";
    public const string UploadSelectedMediaCardTitle = "Текущее локально выбранное видео";
    public const string UploadSelectedMediaSourceLabel = "Источник";
    public const string UploadSelectedMediaFileNameLabel = "Имя файла";
    public const string UploadSelectedMediaContentTypeLabel = "MIME-тип";
    public const string UploadSelectedMediaSelectedAtLabel = "Выбрано";
    public const string UploadSelectedMediaStateLabel = "Состояние";
    public const string UploadSelectedMediaLiveHandleStateText =
        "Локальный доступ к файлу активен только в текущем запуске, без upload и sync.";
    public const string UploadSelectedMediaRestoredMetadataOnlyStateText =
        "После перезапуска восстановлены только локальные метаданные без живого доступа к файлу.";
    public const string UploadSelectedMediaRestoredMetadataNoteText =
        "После перезапуска сохранены только метаданные. Чтобы восстановить локальный доступ или починить восстановленный черновик очереди, выберите тот же видеофайл еще раз.";
    public const string UploadClearSelectionButton = "Очистить локальный выбор";
    public const string UploadClearSelectionResultText = "Локально выбранный медиафайл очищен.";
    public const string UploadUnknownContentTypeText = "Неизвестно";

    public const string LocalDuplicatePrecheckCardTitle = "Локальная предварительная проверка очереди";
    public const string LocalDuplicatePrecheckStatusLabel = "Статус";
    public const string LocalDuplicatePrecheckNoCurrentSelectionText =
        "Сначала выберите или запишите видео. Локальная предварительная проверка очереди пока ничего не сравнивает.";
    public const string LocalDuplicatePrecheckNoKnownDuplicateText =
        "Локальная предварительная проверка не нашла дубликатов в текущей очереди. Это не финальная backend-проверка.";
    public const string LocalDuplicatePrecheckLikelyAlreadyQueuedText =
        "Похоже, выбранное видео уже есть в локальной очереди. Повторная передача заблокирована только локальной предварительной проверкой, это не финальная backend-проверка.";
    public const string LocalDuplicatePrecheckLocalOnlyNote =
        "Сравнение выполняется только по текущему локальному выбору и draft-элементам очереди в памяти устройства.";
    public const string BusinessObjectBindingCardTitle = "Привязка бизнес-объекта для backend precheck";
    public const string BusinessObjectBindingStateLabel = "Статус";
    public const string BusinessObjectBindingUnresolvedStateText = "Не разрешено: approved businessObjectKey отсутствует";
    public const string BusinessObjectBindingUnresolvedWarning =
        "approved businessObjectKey source is not documented yet; backend precheck cannot continue as production flow";
    public const string BusinessObjectBindingSourceDescription =
        "Android хранит только локальный intent. Он не является businessObjectKey и не отправляется в backend.";
    public const string BusinessObjectBindingLocalIntentTitleLabel = "Локальный intent";
    public const string BusinessObjectBindingLocalIntentNoteLabel = "Локальная заметка";
    public const string BusinessObjectBindingDefaultLocalIntentTitle = "Локальный черновик без approved businessObjectKey";
    public const string BusinessObjectBindingSaveLocalIntentButton = "Сохранить локальный intent";
    public const string BusinessObjectBindingClearLocalIntentButton = "Очистить локальный intent";
    public const string BusinessObjectBindingCheckReadinessButton = "Проверить готовность backend precheck";
    public const string BusinessObjectBindingLocalIntentSavedResult = "Локальный intent сохранен. Approved businessObjectKey не создан.";
    public const string BusinessObjectBindingLocalIntentClearedResult = "Локальный intent очищен. Approved businessObjectKey по-прежнему отсутствует.";
    public const string BusinessObjectBindingPreUploadBlockedTitle = "Backend PreUploadCheck заблокирован";
    public const string PreUploadCheckBlockedMessage =
        "approved businessObjectKey source is not documented yet; backend precheck cannot continue as production flow";
    public const string BusinessObjectBindingRequiredActionText =
        "Нужен утвержденный источник businessObjectKey / report draft / business object binding до production PreUploadCheck.";
    public const string BusinessObjectBindingLocalOnlyNote =
        "Это локальная blocker-карточка Android. Она не вызывает backend, не создает create-report и не запускает upload.";
    public const string BusinessObjectBindingNoFakeKeyNote =
        "LocalIntentId не является businessObjectKey. Фейковый ключ, hash файла или group id не используются.";
    public const string BusinessObjectBindingEligibleMessage =
        "Approved businessObjectKey найден. Production PreUploadCheck может быть разрешен отдельной approved integration task.";
    public const string UploadEnqueueStubButton = "Передать выбранное видео в локальную очередь";
    public const string UploadOutboxActionHint =
        "Локальный handoff переносит только текущее выбранное видео в черновик очереди на устройстве. " +
        "После полного перезапуска могут сохраниться только метаданные. Upload, sync и backend-действия не выполняются.";

    public const string QueueTitle = "Очередь";
    public const string QueueIntro =
        "Это локальный экран очереди для mobile foundation baseline. Здесь нет upload, sync и бизнес-действий.";
    public const string QueueFoundationCardTitle = "Локальная карточка очереди";
    public const string QueueFoundationCardSummary =
        "Текущий outbox foundation хранит локальные черновики на устройстве. После полного перезапуска восстанавливаются только метаданные без живого доступа к файлам.";
    public const string QueueLoadingText = "Загружается локальная очередь...";
    public const string QueueEmptyTitle = "Очередь пока пуста";
    public const string QueueEmptyMessage =
        "Добавьте элемент из раздела «Полеты» или со служебного экрана «Загрузка», чтобы проверить локальный foundation очереди.";
    public const string QueueRetryButton = "Повторить (заглушка)";
    public const string QueueRemoveButton = "Удалить";
    public const string QueueRepairButton = "Восстановить локальный доступ к файлу";
    public const string QueueRepairHintText =
        "Если этот черновик восстановлен после перезапуска только по метаданным, выберите тот же видеофайл на экране «Загрузка» и запустите локальное восстановление.";
    public const string QueueCreatedAtLabel = "Создано";
    public const string QueueStatusLabel = "Статус";
    public const string QueueLastActionLabel = "Последнее действие";
    public const string QueueItemTypeLabel = "Тип элемента";
    public const string QueueItemTypeReportDraftText = "Черновик отчета";
    public const string QueueMediaSourceLabel = "Источник видео";
    public const string QueueMediaFileNameLabel = "Имя файла";
    public const string QueueMediaContentTypeLabel = "MIME-тип";
    public const string QueueMediaSelectedAtLabel = "Выбрано";
    public const string QueueMediaDraftStateLabel = "Состояние черновика";
    public const string QueueReportDraftIdLabel = "ID черновика";
    public const string QueueReportAttachmentCountLabel = "Количество вложений";
    public const string QueueReportVideoAttachmentCountLabel = "Видео-вложения";
    public const string QueueReportDraftLocalOnlyNoteText =
        "Это локальная очередь черновика. Backend create-report и businessObjectKey пока не подключены.";
    public const string QueueMediaDraftLocalOnlyText =
        "Локальный media-черновик привязан к файлу только в текущем запуске.";
    public const string QueueMediaDraftRestoredMetadataOnlyText =
        "После перезапуска доступны только локальные метаданные черновика.";
    public const string QueueRestoredMetadataOnlyNoteText =
        "После перезапуска для реального доступа к файлу потребуется повторный выбор и последующая локальная привязка.";
    public const string QueueRepairNoCurrentSelectionText =
        "Сначала выберите или запишите видео. Это только локальная проверка восстановления черновика, а не backend-валидация.";
    public const string QueueRepairSelectionHasNoLiveHandleText =
        "У текущего выбранного видео нет живого локального доступа к файлу. Сначала выберите тот же файл заново, чтобы локально восстановить привязку.";
    public const string QueueRepairNoRepairableDraftText =
        "Для этого элемента сейчас нет локального медиа-черновика, который можно восстановить.";
    public const string QueueRepairAlreadyRepairedText =
        "Локальный доступ к файлу для этого черновика уже восстановлен в текущем запуске.";
    public const string QueueRepairSelectionDoesNotMatchText =
        "Текущее выбранное видео не совпадает с восстановленным черновиком очереди. Это только локальная проверка восстановления, а не backend-валидация.";
    public const string QueueRepairReadyText =
        "Текущее выбранное видео подходит для локального восстановления доступа к файлу в черновике очереди.";
    public const string QueueRepairSuccessLastActionText =
        "Локальный доступ к файлу для черновика восстановлен из текущего выбранного видео.";

    public const string NotFoundTitle = "Страница не найдена";
    public const string NotFoundMessage =
        "Запрошенный экран не найден в текущей локальной оболочке.";

    public const string PendingSyncItemSummary =
        "Элемент создан только для локальной проверки очереди. После перезапуска могут сохраниться только метаданные без upload и sync.";
    public const string PendingSyncEnqueuedLastAction =
        "Элемент добавлен в локальную очередь как заглушка.";
    public const string PendingSyncRetriedLastAction =
        "Выполнен локальный повтор без отправки.";
    public const string PendingSyncItemNotFoundText =
        "Элемент локальной очереди не найден.";
    public const string PendingSyncNoCurrentSelectionText =
        "Сначала выберите или запишите видео, чтобы передать его в локальную очередь.";
    public const string PendingSyncReselectAfterRestartText =
        "После перезапуска восстановлены только локальные метаданные выбранного видео. Чтобы снова передать его в очередь, выберите тот же файл еще раз.";
    public const string PendingSyncMediaDraftSummary =
        "Элемент очереди содержит только локальный медиа-черновик на устройстве без upload, sync и backend-действий. После перезапуска могут остаться только метаданные.";
    public const string PendingSyncMediaDraftEnqueuedLastAction =
        "Выбранное видео передано в локальный черновик очереди.";
    public const string PendingSyncReportDraftSummary =
        "Локальный черновик отчета помещен в очередь только на устройстве. Это не backend create-report, не sync и не upload.";
    public const string PendingSyncReportDraftEnqueuedLastAction =
        "Локальный черновик отчета добавлен в очередь устройства.";
    public const string PendingSyncRestoredMetadataLastActionText =
        "После перезапуска восстановлены только локальные метаданные черновика. Реальный доступ к файлу нужно привязать повторно позже.";

    public const string HomeLandingTitle = "Локальная мобильная оболочка";
    public const string HomeLandingText =
        "Основной локальный сценарий сейчас начинается со списка полетов и черновиков отчетов. Экран загрузки остается служебным и диагностическим.";
    public const string HomeOpenReportsButton = "Перейти в Полеты";

    public const string ReportsStatusStripTitle = "Локальный статус";
    public const string ReportsStatusStripText =
        "Список работает только как report-first baseline: локальные черновики, без backend-сохранения, sync и upload.";
    public const string ReportDraftMediaSectionTitle = "Медиа-вложения отчета";
    public const string ReportDraftPickVideoButton = "Выбрать видео";
    public const string ReportDraftCaptureVideoButton = "Записать видео";
    public const string ReportDraftAttachSelectedVideoButton = "Прикрепить уже выбранное локальное видео";
    public const string ReportDraftPickVideoProgressText =
        "Открывается выбор видео для текущего черновика отчета.";
    public const string ReportDraftCaptureVideoProgressText =
        "Запускается запись видео для текущего черновика отчета.";
    public const string ReportDraftAttachVideoFailureText =
        "Не удалось автоматически прикрепить видео к локальному черновику отчета. Проверьте локальный выбор и повторите действие без backend-вызовов.";
    public const string ReportDraftMediaOperationLoadingText =
        "Локальная media-операция выполняется. После завершения результат будет прикреплен к текущему черновику как метаданные.";
    public const string ReportDraftVideoBlockTitle = "Видео";
    public const string ReportDraftPhotoBlockTitle = "Фото готового дрона";
    public const string ReportDraftLogFileBlockTitle = "Лог-файл";
    public const string ReportDraftFutureAttachmentNote =
        "Этот тип вложения останется локальной заглушкой до отдельного среза без backend-save, upload и финальных контрактов.";

    public const string UploadServiceScreenNote =
        "Служебный экран. Основной сценарий выбора видео находится внутри черновика отчета.";

    public static string GetShellModeText(global::App.Mobile.Android.State.MobileShellMode mode)
    {
        return mode switch
        {
            global::App.Mobile.Android.State.MobileShellMode.Development => "Разработка",
            global::App.Mobile.Android.State.MobileShellMode.GuestPlaceholder => "Гостевой режим-заглушка",
            global::App.Mobile.Android.State.MobileShellMode.OfflineRestrictedPlaceholder => "Ограниченный офлайн-режим-заглушка",
            _ => "Неизвестный режим"
        };
    }

    public static string GetShellBannerText(global::App.Mobile.Android.State.MobileShellMode mode)
    {
        return $"Локальное состояние оболочки: {GetShellModeText(mode)}. " +
               "Это только локальная заглушка shell-state, а не реальная auth/session/backend интеграция.";
    }

    public static string GetMediaCapabilityStateText(global::App.Mobile.Android.Media.MobileMediaCapabilityState state)
    {
        return state switch
        {
            global::App.Mobile.Android.Media.MobileMediaCapabilityState.Unknown => "Неизвестно",
            global::App.Mobile.Android.Media.MobileMediaCapabilityState.StubReady => "Готово только как заглушка",
            global::App.Mobile.Android.Media.MobileMediaCapabilityState.DeviceAvailable => "Доступно на устройстве",
            global::App.Mobile.Android.Media.MobileMediaCapabilityState.NotBoundYet => "Пока не подключено в этом срезе",
            _ => "Неизвестно"
        };
    }

    public static string GetSelectedMediaSourceText(global::App.Mobile.Android.Media.MobileMediaSource source)
    {
        return source switch
        {
            global::App.Mobile.Android.Media.MobileMediaSource.FilePicker => "Файловый выбор",
            global::App.Mobile.Android.Media.MobileMediaSource.GalleryVideo => "Галерея",
            global::App.Mobile.Android.Media.MobileMediaSource.CameraCapture => "Камера",
            _ => "Неизвестно"
        };
    }

    public static string GetUploadSelectedMediaStateText(bool hasLocalReadHandle)
    {
        return hasLocalReadHandle
            ? UploadSelectedMediaLiveHandleStateText
            : UploadSelectedMediaRestoredMetadataOnlyStateText;
    }

    public static string GetLocalDuplicatePrecheckStatusText(
        global::App.Mobile.Android.DuplicatePrecheck.LocalDuplicatePrecheckStatus status)
    {
        return status switch
        {
            global::App.Mobile.Android.DuplicatePrecheck.LocalDuplicatePrecheckStatus.NoCurrentSelection => "Нет выбранного видео",
            global::App.Mobile.Android.DuplicatePrecheck.LocalDuplicatePrecheckStatus.NoKnownDuplicateInOutbox => "Локальных дубликатов не найдено",
            global::App.Mobile.Android.DuplicatePrecheck.LocalDuplicatePrecheckStatus.LikelyAlreadyQueued => "Вероятный дубликат уже в очереди",
            _ => "Неизвестно"
        };
    }

    public static string GetPendingSyncStatusText(global::App.Mobile.Android.Outbox.PendingSyncItemStatus status)
    {
        return status switch
        {
            global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Unknown => "Неизвестно",
            global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued => "В локальной очереди",
            global::App.Mobile.Android.Outbox.PendingSyncItemStatus.RetryRequested => "Локальный повтор запрошен",
            _ => "Неизвестно"
        };
    }

    public static string GetQueueMediaDraftStateText(bool hasLocalReadHandle)
    {
        return hasLocalReadHandle
            ? QueueMediaDraftLocalOnlyText
            : QueueMediaDraftRestoredMetadataOnlyText;
    }

    public static string GetPendingSyncItemTitle(int sequence)
    {
        return $"Локальный элемент очереди #{sequence}";
    }

    public static string GetPendingSyncMediaDraftTitle(string fileName)
    {
        return $"Локальный медиа-черновик: {fileName}";
    }

    public static string GetPendingSyncReportDraftTitle(string draftTitle)
    {
        return $"Локальный черновик отчета: {draftTitle}";
    }

    public static string GetPendingSyncEnqueueResultText(string title)
    {
        return $"Элемент «{title}» добавлен в локальную очередь как заглушка.";
    }

    public static string GetPendingSyncRetryResultText(string title)
    {
        return $"Для элемента «{title}» выполнен локальный повтор без отправки.";
    }

    public static string GetPendingSyncRemoveResultText(string title)
    {
        return $"Элемент «{title}» удален из локальной очереди.";
    }

    public static string GetPendingSyncMediaDraftHandoffResultText(string fileName)
    {
        return $"Видео «{fileName}» передано в локальный черновик очереди.";
    }

    public static string GetQueueRepairSuccessText(string fileName)
    {
        return $"Для черновика с видео «{fileName}» локальный доступ к файлу восстановлен.";
    }

    public static string GetUploadNativePickerSuccessText(string fileName)
    {
        return $"Выбрано видео «{fileName}». Файл не был скопирован, сохранен отдельно или отправлен.";
    }

    public static string GetUploadNativeCaptureSuccessText(string fileName)
    {
        return $"Записано видео «{fileName}». Файл не был скопирован, сохранен отдельно или отправлен.";
    }

    public static string GetReportDraftStatusText(global::App.Mobile.Android.Reports.MobileReportDraftStatus status)
    {
        return status switch
        {
            global::App.Mobile.Android.Reports.MobileReportDraftStatus.Draft => "Черновик",
            global::App.Mobile.Android.Reports.MobileReportDraftStatus.ReadyForAttachmentReview => "Готов к просмотру вложений",
            global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal => "В локальной очереди",
            _ => "Неизвестно"
        };
    }

    public static string GetReportAttachmentKindText(global::App.Mobile.Android.Reports.MobileReportAttachmentKind kind)
    {
        return kind switch
        {
            global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video => "Видео",
            global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Photo => "Фото",
            global::App.Mobile.Android.Reports.MobileReportAttachmentKind.LogFile => "Лог-файл",
            _ => "Неизвестно"
        };
    }

    public static string GetReportAttachmentLocalAccessText(bool hasLocalReadHandle)
    {
        return hasLocalReadHandle
            ? ReportAttachmentLocalAccessAvailableText
            : ReportAttachmentLocalAccessMissingText;
    }

    public static string GetReportDraftTitle(int sequence)
    {
        return $"FPV-отчет #{sequence}";
    }

    public static string GetLookupStubOptionText(int sequence)
    {
        return $"Заглушка — значение {sequence}";
    }

    public static string GetReportDraftFieldUpdatedText(string label, string? valueText)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(valueText)
            ? "пустое локальное значение"
            : valueText.Trim();

        return $"Поле «{label}» локально обновлено: {normalizedValue}.";
    }

    public static string GetReportDraftFieldEditorPlaceholderText(
        global::App.Mobile.Android.Lookup.MobileLookupFieldKind fieldKind)
    {
        return fieldKind switch
        {
            global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Number => ReportDraftNumericValuePlaceholder,
            global::App.Mobile.Android.Lookup.MobileLookupFieldKind.DateTime => ReportDraftDateTimeValuePlaceholder,
            _ => ReportDraftTextValuePlaceholder
        };
    }

    public static string GetToggleValueText(bool value)
    {
        return value ? ReportDraftToggleYesText : ReportDraftToggleNoText;
    }

    public static string GetReportDraftAttachVideoSuccessText(string fileName)
    {
        return $"Видео «{fileName}» автоматически прикреплено к локальному черновику как метаданные вложения.";
    }

    public static string GetReportDraftQueuedLocalText(string title)
    {
        return $"Отчет «{title}» помещен в локальную очередь черновиков.";
    }
}