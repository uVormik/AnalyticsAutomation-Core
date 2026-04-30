using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionViewModel(IDesktopVideoFilePicker videoFilePicker)
{
    private readonly IDesktopVideoFilePicker _videoFilePicker =
        videoFilePicker ?? throw new ArgumentNullException(nameof(videoFilePicker));

    public DesktopWorkspaceSection CurrentSection { get; private set; } = DesktopWorkspaceSection.Workspace;

    public bool IsUploadSectionOpen => CurrentSection == DesktopWorkspaceSection.Upload;

    public bool IsSelectingFile { get; private set; }

    public DesktopUploadSelectedFile? SelectedFile { get; private set; }

    public string SelectionStatusMessage { get; private set; } =
        DesktopUploadSectionText.PlaceholderResult;

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

        try
        {
            DesktopVideoFilePickerResult result = await _videoFilePicker.PickVideoFileAsync(cancellationToken);

            if (result.Status == DesktopVideoFilePickerStatus.Selected && result.SelectedFile is not null)
            {
                SelectedFile = result.SelectedFile;
                SelectionStatusMessage = DesktopUploadSectionText.SelectedFilePreviewMessage;
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

    private void ResetSelection()
    {
        SelectedFile = null;
        SelectionStatusMessage = DesktopUploadSectionText.PlaceholderResult;
    }
}