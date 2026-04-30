using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionViewModel(
    IDesktopVideoFilePicker videoFilePicker,
    IDesktopVideoHashService videoHashService,
    DesktopUploadSectionOptions uploadSectionOptions)
{
    public DesktopUploadSectionViewModel(
        IDesktopVideoFilePicker videoFilePicker,
        IDesktopVideoHashService videoHashService)
        : this(videoFilePicker, videoHashService, DesktopUploadSectionOptions.Disabled)
    {
    }

    private readonly IDesktopVideoFilePicker _videoFilePicker =
        videoFilePicker ?? throw new ArgumentNullException(nameof(videoFilePicker));
    private readonly IDesktopVideoHashService _videoHashService =
        videoHashService ?? throw new ArgumentNullException(nameof(videoHashService));
    private readonly DesktopUploadSectionOptions _uploadSectionOptions =
        uploadSectionOptions ?? throw new ArgumentNullException(nameof(uploadSectionOptions));

    private DesktopVideoHashRequest? _selectedFileHashRequest;

    public DesktopWorkspaceSection CurrentSection { get; private set; } = DesktopWorkspaceSection.Workspace;

    public bool IsUploadSectionOpen => CurrentSection == DesktopWorkspaceSection.Upload;

    public bool IsSelectingFile { get; private set; }

    public bool IsHashing { get; private set; }

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

    public string BusinessObjectKeyInput { get; set; } = string.Empty;

    public DesktopUploadBusinessObjectKey? BusinessObjectKey { get; private set; }

    public bool HasBusinessObjectKey => BusinessObjectKey is not null;

    public string? BusinessObjectKeyPreview => BusinessObjectKey?.Value;

    public string BusinessObjectKeyStatusMessage { get; private set; } =
        DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage;

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

    private void ResetSelection()
    {
        SelectedFile = null;
        SelectionStatusMessage = DesktopUploadSectionText.PlaceholderResult;
        ResetSelectedFileHashState();
        ResetBusinessObjectKeyState();
    }

    private void ResetSelectedFileHashState()
    {
        _selectedFileHashRequest = null;
        IsHashing = false;
        Sha256Hex = null;
        HashStatusMessage = DesktopUploadSectionText.HashNotReadyMessage;
    }

    private void ResetBusinessObjectKeyState()
    {
        BusinessObjectKeyInput = string.Empty;
        BusinessObjectKey = null;
        BusinessObjectKeyStatusMessage = DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage;
    }
}