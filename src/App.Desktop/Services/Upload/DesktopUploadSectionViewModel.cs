using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionViewModel(
    IDesktopVideoFilePicker videoFilePicker,
    IDesktopVideoHashService videoHashService,
    IDesktopPreUploadCheckClient preUploadCheckClient,
    IDesktopDirectSiteUploadClient directSiteUploadClient,
    DesktopUploadSectionOptions uploadSectionOptions)
{
    public DesktopUploadSectionViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService)
        : this(
            videoFilePicker,
            videoHashService,
            new DisabledDesktopPreUploadCheckClient(),
            new DisabledDesktopDirectSiteUploadClient(),
            DesktopUploadSectionOptions.Disabled)
    {
    }

    public DesktopUploadSectionViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        DesktopUploadSectionOptions uploadSectionOptions)
        : this(
            videoFilePicker,
            videoHashService,
            new DisabledDesktopPreUploadCheckClient(),
            new DisabledDesktopDirectSiteUploadClient(),
            uploadSectionOptions)
    {
    }

    private readonly IDesktopVideoFilePicker _videoFilePicker =
        videoFilePicker ?? throw new ArgumentNullException(nameof(videoFilePicker));
    private readonly IDesktopVideoHashService _videoHashService =
        videoHashService ?? throw new ArgumentNullException(nameof(videoHashService));
    private readonly IDesktopPreUploadCheckClient _preUploadCheckClient =
        preUploadCheckClient ?? throw new ArgumentNullException(nameof(preUploadCheckClient));
    private readonly IDesktopDirectSiteUploadClient _directSiteUploadClient =
        directSiteUploadClient ?? throw new ArgumentNullException(nameof(directSiteUploadClient));
    private readonly DesktopUploadSectionOptions _uploadSectionOptions =
        uploadSectionOptions ?? throw new ArgumentNullException(nameof(uploadSectionOptions));

    private DesktopVideoHashRequest? _selectedFileHashRequest;

    public DesktopWorkspaceSection CurrentSection { get; private set; } = DesktopWorkspaceSection.Workspace;

    public bool IsUploadSectionOpen => CurrentSection == DesktopWorkspaceSection.Upload;

    public bool IsSelectingFile { get; private set; }

    public bool IsHashing { get; private set; }

    public bool IsCheckingPreUpload { get; private set; }

    public bool IsUploadingToSite { get; private set; }

    public bool CanCalculateHash => SelectedFile is not null && !IsSelectingFile && !IsHashing;

    public DesktopUploadSelectedFile? SelectedFile { get; private set; }

    public string SelectionStatusMessage { get; private set; } =
        DesktopUploadSectionText.PlaceholderResult;

    public string HashStatusMessage { get; private set; } =
        DesktopUploadSectionText.HashNotReadyMessage;

    public string? Sha256Hex { get; private set; }

    public bool HasSha256Hash => Sha256Hex is not null;

    public bool IsDevFakeBusinessObjectKeyEnabled =>
        _uploadSectionOptions.IsDevFakeBusinessObjectKeyEnabled;

    public bool IsDevFakePreUploadCheckEnabled =>
        _uploadSectionOptions.IsDevFakePreUploadCheckEnabled;

    public bool IsDevFakeSiteUploadEnabled =>
        _uploadSectionOptions.IsDevFakeSiteUploadEnabled;

    public string BusinessObjectKeyInput { get; set; } = string.Empty;

    public DesktopUploadBusinessObjectKey? BusinessObjectKey { get; private set; }

    public bool HasBusinessObjectKey => BusinessObjectKey is not null;

    public string? BusinessObjectKeyPreview => BusinessObjectKey?.Value;

    public string BusinessObjectKeyStatusMessage { get; private set; } =
        DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage;

    public DesktopPreUploadCheckRequestPreview? PreUploadCheckRequestPreview =>
        DesktopPreUploadCheckRequestPreview.TryCreate(SelectedFile, Sha256Hex, BusinessObjectKey);

    public bool HasPreUploadCheckRequestPreview => PreUploadCheckRequestPreview is not null;

    public bool CanRequestPreUploadCheck =>
        HasPreUploadCheckRequestPreview && !IsSelectingFile && !IsHashing && !IsCheckingPreUpload;

    public DesktopPreUploadCheckResult? PreUploadCheckResult { get; private set; }

    public bool HasPreUploadCheckDecision => PreUploadCheckResult?.Decision is not null;

    public string? PreUploadCheckDecisionPreview => PreUploadCheckResult?.DecisionPreview;

    public string PreUploadCheckStatusMessage { get; private set; } =
        DesktopUploadSectionText.PreUploadCheckNotReadyMessage;

    public DesktopSiteUploadRequestPreview? SiteUploadRequestPreview =>
        DesktopSiteUploadRequestPreview.TryCreate(PreUploadCheckRequestPreview, PreUploadCheckResult);

    public bool HasSiteUploadRequestPreview => SiteUploadRequestPreview is not null;

    public bool CanUploadToSite =>
        HasSiteUploadRequestPreview
        && !IsSelectingFile
        && !IsHashing
        && !IsCheckingPreUpload
        && !IsUploadingToSite;

    public DesktopSiteUploadResult? SiteUploadResult { get; private set; }

    public bool HasSiteUploadResult => SiteUploadResult?.Status == DesktopSiteUploadStatus.Succeeded;

    public string? SiteUploadStatusPreview => SiteUploadResult?.StatusPreview;

    public string? SiteUploadExternalVideoId => SiteUploadResult?.ExternalVideoId;

    public string? SiteUploadSiteStorageKey => SiteUploadResult?.SiteStorageKey;

    public string SiteUploadStatusMessage { get; private set; } =
        DesktopUploadSectionText.SiteUploadNotReadyMessage;

    public void OpenUploadSection()
    {
        CurrentSection = DesktopWorkspaceSection.Upload;
    }

    public void BackToWorkspace()
    {
        CurrentSection = DesktopWorkspaceSection.Workspace;
        ResetSelection();
    }

    public void ResetForSignedOutState()
    {
        CurrentSection = DesktopWorkspaceSection.Workspace;
        ResetSelection();
    }

    public async ValueTask<DesktopUploadSelectedFile?> SelectVideoFileAsync(CancellationToken cancellationToken)
    {
        if (IsSelectingFile)
        {
            return SelectedFile;
        }

        IsSelectingFile = true;
        SelectionStatusMessage = DesktopUploadSectionText.SelectingFileMessage;
        ResetSelectedFileHashState();
        ResetBusinessObjectKeyState();
        ResetPreUploadCheckState();
        ResetSiteUploadState();

        try
        {
            DesktopVideoFilePickerResult result = await _videoFilePicker.PickVideoFileAsync(cancellationToken);

            if (result.Status == DesktopVideoFilePickerStatus.Selected && result.SelectedFile is not null)
            {
                SelectedFile = result.SelectedFile;
                _selectedFileHashRequest = result.HashSource is null
                    ? null
                    : DesktopVideoHashRequest.FromSource(result.HashSource);
                SelectionStatusMessage = DesktopUploadSectionText.SelectedFilePreviewMessage;
                HashStatusMessage = DesktopUploadSectionText.HashReadyMessage;
                RefreshPreUploadCheckReadiness();
                return SelectedFile;
            }

            SelectedFile = null;
            SelectionStatusMessage = result.Status == DesktopVideoFilePickerStatus.Unavailable
                ? DesktopUploadSectionText.SelectionUnavailableMessage
                : DesktopUploadSectionText.SelectionCanceledMessage;
            return null;
        }
        finally
        {
            IsSelectingFile = false;
        }
    }

    public async ValueTask<DesktopVideoHashResult?> CalculateSha256Async(CancellationToken cancellationToken)
    {
        if (!CanCalculateHash)
        {
            HashStatusMessage = DesktopUploadSectionText.HashNotReadyMessage;
            return null;
        }

        IsHashing = true;
        Sha256Hex = null;
        HashStatusMessage = DesktopUploadSectionText.HashInProgressMessage;
        ResetPreUploadCheckState();
        ResetSiteUploadState();

        try
        {
            if (_selectedFileHashRequest is null)
            {
                HashStatusMessage = DesktopUploadSectionText.HashUnavailableMessage;
                return DesktopVideoHashResult.Unavailable;
            }

            DesktopVideoHashResult result = await _videoHashService.CalculateSha256Async(
                _selectedFileHashRequest,
                cancellationToken);

            if (result.Status == DesktopVideoHashStatus.Succeeded && result.Sha256Hex is not null)
            {
                Sha256Hex = result.Sha256Hex;
                HashStatusMessage = DesktopUploadSectionText.HashSucceededMessage;
                RefreshPreUploadCheckReadiness();
                return result;
            }

            HashStatusMessage = result.Status switch
            {
                DesktopVideoHashStatus.Canceled => DesktopUploadSectionText.HashCanceledMessage,
                _ => DesktopUploadSectionText.HashUnavailableMessage
            };

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            HashStatusMessage = DesktopUploadSectionText.HashCanceledMessage;
            RefreshPreUploadCheckReadiness();
            return DesktopVideoHashResult.Canceled;
        }
        finally
        {
            IsHashing = false;
        }
    }

    public DesktopUploadBusinessObjectKeyValidation ApplyBusinessObjectKey()
    {
        DesktopUploadBusinessObjectKeyValidation validation =
            DesktopUploadBusinessObjectKeyValidator.ValidateManualInput(BusinessObjectKeyInput);

        BusinessObjectKeyInput = validation.SafeInputValue;
        BusinessObjectKey = validation.BusinessObjectKey;
        BusinessObjectKeyStatusMessage = validation.Message;
        ResetPreUploadCheckDecision();
        ResetSiteUploadState();
        RefreshPreUploadCheckReadiness();

        return validation;
    }

    public DesktopUploadBusinessObjectKeyValidation? UseDevFakeBusinessObjectKey()
    {
        if (!IsDevFakeBusinessObjectKeyEnabled)
        {
            return null;
        }

        BusinessObjectKeyInput = DesktopUploadBusinessObjectKey.VisualSmokeValue;
        return ApplyBusinessObjectKey();
    }

    public async ValueTask<DesktopPreUploadCheckResult?> CheckPreUploadAsync(CancellationToken cancellationToken)
    {
        if (IsCheckingPreUpload)
        {
            return PreUploadCheckResult;
        }

        DesktopPreUploadCheckRequestPreview? requestPreview = PreUploadCheckRequestPreview;
        if (requestPreview is null)
        {
            PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckNotReadyMessage;
            ResetSiteUploadState();
            return null;
        }

        if (!IsDevFakePreUploadCheckEnabled)
        {
            PreUploadCheckResult = DesktopPreUploadCheckResult.Deferred;
            PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckDeferredMessage;
            RefreshSiteUploadReadiness();
            return PreUploadCheckResult;
        }

        IsCheckingPreUpload = true;
        PreUploadCheckResult = null;
        PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckInProgressMessage;
        ResetSiteUploadState();

        try
        {
            PreUploadCheckResult = await _preUploadCheckClient.CheckAsync(requestPreview, cancellationToken);
            PreUploadCheckStatusMessage = PreUploadCheckResult.Message;
            RefreshSiteUploadReadiness();
            return PreUploadCheckResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            PreUploadCheckResult = DesktopPreUploadCheckResult.Canceled;
            PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckCanceledMessage;
            RefreshSiteUploadReadiness();
            return PreUploadCheckResult;
        }
        finally
        {
            IsCheckingPreUpload = false;
        }
    }

    public async ValueTask<DesktopSiteUploadResult?> UploadToSiteAsync(CancellationToken cancellationToken)
    {
        if (IsUploadingToSite)
        {
            return SiteUploadResult;
        }

        DesktopSiteUploadRequestPreview? requestPreview = SiteUploadRequestPreview;
        if (requestPreview is null)
        {
            SiteUploadStatusMessage = GetSiteUploadNotReadyMessage();
            return null;
        }

        if (!IsDevFakeSiteUploadEnabled)
        {
            SiteUploadResult = DesktopSiteUploadResult.Deferred;
            SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadDeferredMessage;
            return SiteUploadResult;
        }

        IsUploadingToSite = true;
        SiteUploadResult = null;
        SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadInProgressMessage;

        try
        {
            SiteUploadResult = await _directSiteUploadClient.UploadAsync(requestPreview, cancellationToken);
            SiteUploadStatusMessage = SiteUploadResult.Message;
            return SiteUploadResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SiteUploadResult = DesktopSiteUploadResult.Canceled;
            SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadCanceledMessage;
            return SiteUploadResult;
        }
        finally
        {
            IsUploadingToSite = false;
        }
    }

    private void ResetSelection()
    {
        SelectedFile = null;
        SelectionStatusMessage = DesktopUploadSectionText.PlaceholderResult;
        ResetSelectedFileHashState();
        ResetBusinessObjectKeyState();
        ResetPreUploadCheckState();
    }

    private void ResetSelectedFileHashState()
    {
        _selectedFileHashRequest = null;
        IsHashing = false;
        Sha256Hex = null;
        HashStatusMessage = DesktopUploadSectionText.HashNotReadyMessage;
        ResetPreUploadCheckState();
    }

    private void ResetBusinessObjectKeyState()
    {
        BusinessObjectKeyInput = string.Empty;
        BusinessObjectKey = null;
        BusinessObjectKeyStatusMessage = DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage;
        ResetPreUploadCheckState();
        ResetSiteUploadState();
    }

    private void ResetPreUploadCheckState()
    {
        IsCheckingPreUpload = false;
        PreUploadCheckResult = null;
        PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckNotReadyMessage;
        ResetSiteUploadState();
    }

    private void ResetPreUploadCheckDecision()
    {
        IsCheckingPreUpload = false;
        PreUploadCheckResult = null;
        ResetSiteUploadState();
    }

    private void RefreshPreUploadCheckReadiness()
    {
        if (IsCheckingPreUpload)
        {
            return;
        }

        PreUploadCheckStatusMessage = HasPreUploadCheckRequestPreview
            ? DesktopUploadSectionText.PreUploadCheckReadyMessage
            : DesktopUploadSectionText.PreUploadCheckNotReadyMessage;
    }

    private void ResetSiteUploadState()
    {
        IsUploadingToSite = false;
        SiteUploadResult = null;
        SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadNotReadyMessage;
    }

    private void RefreshSiteUploadReadiness()
    {
        if (IsUploadingToSite)
        {
            return;
        }

        SiteUploadStatusMessage = SiteUploadRequestPreview is not null
            ? DesktopUploadSectionText.SiteUploadReadyMessage
            : GetSiteUploadNotReadyMessage();
    }

    private string GetSiteUploadNotReadyMessage()
    {
        return PreUploadCheckResult?.Decision is DesktopPreUploadCheckDecision.BlockHardDuplicate
            or DesktopPreUploadCheckDecision.BlockPossibleFalsification
            ? DesktopUploadSectionText.SiteUploadBlockedByPreUploadCheckMessage
            : DesktopUploadSectionText.SiteUploadNotReadyMessage;
    }
}