using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionViewModel(DesktopUploadSectionOptions options)
{
    private readonly DesktopUploadSectionOptions _options =
        options ?? throw new ArgumentNullException(nameof(options));

    public DesktopWorkspaceSection CurrentSection { get; private set; } = DesktopWorkspaceSection.Workspace;

    public bool IsUploadSectionOpen => CurrentSection == DesktopWorkspaceSection.Upload;

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
    }

    public void ResetForSignedOutState()
    {
        CurrentSection = DesktopWorkspaceSection.Workspace;
        SelectedFile = null;
        SelectionStatusMessage = DesktopUploadSectionText.PlaceholderResult;
    }

    public DesktopUploadSelectedFile? SelectVideoFilePlaceholder()
    {
        SelectionStatusMessage = DesktopUploadSectionText.PlaceholderResult;

        if (!_options.IsDevFakeUploadFileEnabled)
        {
            SelectedFile = null;
            return SelectedFile;
        }

        SelectedFile = DesktopUploadSelectedFile.VisualSmokeFile;
        return SelectedFile;
    }
}