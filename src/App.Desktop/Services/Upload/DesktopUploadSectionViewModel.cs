using App.Desktop.Boundaries;
using App.Desktop.Services.GroupTree;

namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionViewModel(
    IDesktopVideoFilePicker videoFilePicker,
    IDesktopVideoHashService videoHashService,
    IDesktopPreUploadCheckClient preUploadCheckClient,
    IDesktopDirectSiteUploadClient directSiteUploadClient,
    IDesktopUploadReceiptClient uploadReceiptClient,
    DesktopGroupSelectionState groupSelectionState,
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
            new DisabledDesktopUploadReceiptClient(),
            new DesktopGroupSelectionState(),
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
            new DisabledDesktopUploadReceiptClient(),
            new DesktopGroupSelectionState(),
            uploadSectionOptions)
    {
    }

    public DesktopUploadSectionViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        IDesktopPreUploadCheckClient preUploadCheckClient,
        IDesktopDirectSiteUploadClient directSiteUploadClient,
        DesktopUploadSectionOptions uploadSectionOptions)
        : this(
            videoFilePicker,
            videoHashService,
            preUploadCheckClient,
            directSiteUploadClient,
            new DisabledDesktopUploadReceiptClient(),
            new DesktopGroupSelectionState(),
            uploadSectionOptions)
    {
    }

    public DesktopUploadSectionViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService,
        IDesktopPreUploadCheckClient preUploadCheckClient,
        IDesktopDirectSiteUploadClient directSiteUploadClient,
        IDesktopUploadReceiptClient uploadReceiptClient,
        DesktopUploadSectionOptions uploadSectionOptions)
        : this(
            videoFilePicker,
            videoHashService,
            preUploadCheckClient,
            directSiteUploadClient,
            uploadReceiptClient,
            new DesktopGroupSelectionState(),
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
    private readonly IDesktopUploadReceiptClient _uploadReceiptClient =
        uploadReceiptClient ?? throw new ArgumentNullException(nameof(uploadReceiptClient));
    private readonly DesktopGroupSelectionState _groupSelectionState =
        groupSelectionState ?? throw new ArgumentNullException(nameof(groupSelectionState));
    private readonly DesktopUploadSectionOptions _uploadSectionOptions =
        uploadSectionOptions ?? throw new ArgumentNullException(nameof(uploadSectionOptions));

    private DesktopVideoHashRequest? _selectedFileHashRequest;

    public DesktopWorkspaceSection CurrentSection { get; private set; } = DesktopWorkspaceSection.Workspace;

    public bool IsUploadSectionOpen => CurrentSection == DesktopWorkspaceSection.Upload;

    public bool IsGroupTreeSectionOpen => CurrentSection == DesktopWorkspaceSection.GroupTree;

    public bool IsSelectingFile { get; private set; }

    public bool IsHashing { get; private set; }

    public bool IsCheckingPreUpload { get; private set; }

    public bool IsUploadingToSite { get; private set; }

    public bool IsCreatingUploadReceipt { get; private set; }

    public bool CanCalculateHash => SelectedFile is not null && !IsSelectingFile && !IsHashing;

    public DesktopSelectedGroupContext? SelectedGroupContext => _groupSelectionState.SelectedGroup;

    public bool HasSelectedGroupContext => SelectedGroupContext is not null;

    public string GroupContextStatusMessage => HasSelectedGroupContext
        ? string.Empty
        : DesktopUploadSectionText.GroupContextMissingMessage;

    public string? SelectedGroupDisplayName => SelectedGroupContext?.DisplayName;

    public string? SelectedGroupId => SelectedGroupContext?.Id;

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

    public bool IsLiveControlPlanePreUploadCheckEnabled =>
        _uploadSectionOptions.IsLiveControlPlanePreUploadCheckEnabled;

    public bool IsDevFakeSiteUploadEnabled =>
        _uploadSectionOptions.IsDevFakeSiteUploadEnabled;

    public bool IsDevFakeUploadReceiptEnabled =>
        _uploadSectionOptions.IsDevFakeUploadReceiptEnabled;

    public string BusinessObjectKeyInput { get; set; } = string.Empty;

    public DesktopUploadBusinessObjectKey? BusinessObjectKey { get; private set; }

    public bool HasBusinessObjectKey => BusinessObjectKey is not null;

    public string? BusinessObjectKeyPreview => BusinessObjectKey?.Value;

    public string BusinessObjectKeyStatusMessage { get; private set; } =
        DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage;

    public DesktopPreUploadCheckRequestPreview? PreUploadCheckRequestPreview =>
        DesktopPreUploadCheckRequestPreview.TryCreate(
            SelectedFile,
            Sha256Hex,
            BusinessObjectKey,
            SelectedGroupContext);

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

    public DesktopUploadReceiptRequestPreview? UploadReceiptRequestPreview =>
        DesktopUploadReceiptRequestPreview.TryCreate(SiteUploadRequestPreview, SiteUploadResult);

    public bool HasUploadReceiptRequestPreview => UploadReceiptRequestPreview is not null;

    public bool CanCreateUploadReceipt =>
        HasUploadReceiptRequestPreview
        && !IsSelectingFile
        && !IsHashing
        && !IsCheckingPreUpload
        && !IsUploadingToSite
        && !IsCreatingUploadReceipt;

    public DesktopUploadReceiptResult? UploadReceiptResult { get; private set; }

    public bool HasUploadReceiptResult => UploadReceiptResult?.Status == DesktopUploadReceiptStatus.Accepted;

    public string? UploadReceiptStatusPreview => UploadReceiptResult?.StatusPreview;

    public string? UploadReceiptId => UploadReceiptResult?.ReceiptId;

    public string? UploadReceiptServerCorrelationId => UploadReceiptResult?.ServerCorrelationId;

    public string UploadReceiptStatusMessage { get; private set; } =
        DesktopUploadSectionText.UploadReceiptNotReadyMessage;

    public void OpenUploadSection()
    {
        CurrentSection = DesktopWorkspaceSection.Upload;
    }

    public void OpenGroupTreeSection()
    {
        CurrentSection = DesktopWorkspaceSection.GroupTree;
    }

    public void BackToWorkspace()
    {
        CurrentSection = DesktopWorkspaceSection.Workspace;
        ResetSelection();
        _groupSelectionState.Clear();
    }

    public void ResetForSignedOutState()
    {
        CurrentSection = DesktopWorkspaceSection.Workspace;
        ResetSelection();
        _groupSelectionState.Clear();
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
        ResetUploadReceiptState();

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
        ResetUploadReceiptState();

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
        ResetUploadReceiptState();
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

        if (!IsDevFakePreUploadCheckEnabled && !IsLiveControlPlanePreUploadCheckEnabled)
        {
            PreUploadCheckResult = DesktopPreUploadCheckResult.Deferred;
            PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckDeferredMessage;
            RefreshSiteUploadReadiness();
            return PreUploadCheckResult;
        }

        IsCheckingPreUpload = true;
        PreUploadCheckResult = null;
        PreUploadCheckStatusMessage = IsLiveControlPlanePreUploadCheckEnabled
            ? DesktopUploadSectionText.PreUploadCheckLiveInProgressMessage
            : DesktopUploadSectionText.PreUploadCheckInProgressMessage;
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
            RefreshUploadReceiptReadiness();
            return SiteUploadResult;
        }

        IsUploadingToSite = true;
        SiteUploadResult = null;
        SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadInProgressMessage;
        ResetUploadReceiptState();

        try
        {
            SiteUploadResult = await _directSiteUploadClient.UploadAsync(requestPreview, cancellationToken);
            SiteUploadStatusMessage = SiteUploadResult.Message;
            RefreshUploadReceiptReadiness();
            return SiteUploadResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SiteUploadResult = DesktopSiteUploadResult.Canceled;
            SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadCanceledMessage;
            RefreshUploadReceiptReadiness();
            return SiteUploadResult;
        }
        finally
        {
            IsUploadingToSite = false;
        }
    }

    public async ValueTask<DesktopUploadReceiptResult?> CreateUploadReceiptAsync(CancellationToken cancellationToken)
    {
        if (IsCreatingUploadReceipt)
        {
            return UploadReceiptResult;
        }

        DesktopUploadReceiptRequestPreview? requestPreview = UploadReceiptRequestPreview;
        if (requestPreview is null)
        {
            UploadReceiptStatusMessage = DesktopUploadSectionText.UploadReceiptNotReadyMessage;
            return null;
        }

        if (!IsDevFakeUploadReceiptEnabled)
        {
            UploadReceiptResult = DesktopUploadReceiptResult.Deferred;
            UploadReceiptStatusMessage = DesktopUploadSectionText.UploadReceiptDeferredMessage;
            return UploadReceiptResult;
        }

        IsCreatingUploadReceipt = true;
        UploadReceiptResult = null;
        UploadReceiptStatusMessage = DesktopUploadSectionText.UploadReceiptInProgressMessage;

        try
        {
            UploadReceiptResult = await _uploadReceiptClient.CreateAsync(requestPreview, cancellationToken);
            UploadReceiptStatusMessage = UploadReceiptResult.Message;
            return UploadReceiptResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UploadReceiptResult = DesktopUploadReceiptResult.Canceled;
            UploadReceiptStatusMessage = DesktopUploadSectionText.UploadReceiptCanceledMessage;
            return UploadReceiptResult;
        }
        finally
        {
            IsCreatingUploadReceipt = false;
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
        ResetUploadReceiptState();
    }

    private void ResetPreUploadCheckState()
    {
        IsCheckingPreUpload = false;
        PreUploadCheckResult = null;
        PreUploadCheckStatusMessage = DesktopUploadSectionText.PreUploadCheckNotReadyMessage;
        ResetSiteUploadState();
        ResetUploadReceiptState();
    }

    private void ResetPreUploadCheckDecision()
    {
        IsCheckingPreUpload = false;
        PreUploadCheckResult = null;
        ResetSiteUploadState();
        ResetUploadReceiptState();
    }

    private void RefreshPreUploadCheckReadiness()
    {
        if (IsCheckingPreUpload)
        {
            return;
        }

        PreUploadCheckStatusMessage = HasPreUploadCheckRequestPreview
            ? GetPreUploadCheckReadyMessage()
            : DesktopUploadSectionText.PreUploadCheckNotReadyMessage;
    }

    private void ResetSiteUploadState()
    {
        IsUploadingToSite = false;
        SiteUploadResult = null;
        SiteUploadStatusMessage = DesktopUploadSectionText.SiteUploadNotReadyMessage;
        ResetUploadReceiptState();
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
        RefreshUploadReceiptReadiness();
    }

    private void ResetUploadReceiptState()
    {
        IsCreatingUploadReceipt = false;
        UploadReceiptResult = null;
        UploadReceiptStatusMessage = DesktopUploadSectionText.UploadReceiptNotReadyMessage;
    }

    private void RefreshUploadReceiptReadiness()
    {
        if (IsCreatingUploadReceipt)
        {
            return;
        }

        UploadReceiptStatusMessage = UploadReceiptRequestPreview is not null
            ? DesktopUploadSectionText.UploadReceiptReadyMessage
            : DesktopUploadSectionText.UploadReceiptNotReadyMessage;
    }

    private string GetSiteUploadNotReadyMessage()
    {
        return PreUploadCheckResult?.Decision is DesktopPreUploadCheckDecision.BlockHardDuplicate
            or DesktopPreUploadCheckDecision.BlockPossibleFalsification
            ? DesktopUploadSectionText.SiteUploadBlockedByPreUploadCheckMessage
            : DesktopUploadSectionText.SiteUploadNotReadyMessage;
    }

    private string GetPreUploadCheckReadyMessage()
    {
        return IsLiveControlPlanePreUploadCheckEnabled
            ? DesktopUploadSectionText.PreUploadCheckLiveReadyMessage
            : DesktopUploadSectionText.PreUploadCheckReadyMessage;
    }
}