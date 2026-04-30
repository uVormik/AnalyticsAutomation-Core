using App.Desktop.Boundaries;
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
        var viewModel = new DesktopUploadSectionViewModel(new CancelingDesktopVideoFilePicker());

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
        Assert.Equal("Шаг 1. Выбор видеофайла", DesktopUploadSectionText.StepOneTitle);
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
    }

    [Fact]
    public void UploadSectionDoesNotReferenceApiHashingOrUploadFlowBoundaries()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Components", "DesktopShell.razor"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionViewModel.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionOptions.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopVideoFilePicker.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "WpfDesktopVideoFilePicker.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopVideoFilePickerBoundary.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("App.Api", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IControlPlaneApiClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IDesktopUploadOrchestrator", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadSha256", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SHA-256", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sha256", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("businessObjectKey", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PreUploadCheck", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DirectSiteUpload", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UploadReceipt", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ReadAllBytes", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("OpenRead", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FakeUploadFileSelectionIsDevOnlyAndDisabledByDefault()
    {
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false").IsDevFakeUploadFileEnabled);

#if DEBUG
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true").IsDevFakeUploadFileEnabled);
#else
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true").IsDevFakeUploadFileEnabled);
#endif
    }

    [Fact]
    public async Task CancelSelectionShowsSafeRussianMessage()
    {
        var viewModel = new DesktopUploadSectionViewModel(new CancelingDesktopVideoFilePicker());

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.Null(selectedFile);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.SelectionCanceledMessage, viewModel.SelectionStatusMessage);
        Assert.False(viewModel.IsSelectingFile);
    }

    [Fact]
    public async Task FakeSelectedFileStateIsSafeAndContainsNoRealPathOrSecret()
    {
        var viewModel = new DesktopUploadSectionViewModel(new FakeDesktopVideoFilePicker());

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.NotNull(selectedFile);
        Assert.Equal("visual-smoke-video.mp4", selectedFile.FileName);
        Assert.Equal(12345678, selectedFile.SizeBytes);
        Assert.Equal("video/mp4", selectedFile.ContentType);
        Assert.Equal(DesktopUploadSectionText.SelectedFilePreviewMessage, viewModel.SelectionStatusMessage);
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
        var viewModel = new DesktopUploadSectionViewModel(new StaticDesktopVideoFilePicker(
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
        var viewModel = new DesktopUploadSectionViewModel(new StaticDesktopVideoFilePicker(
            DesktopVideoFilePickerResult.Selected("safe-preview.mp4", 42, "Authorization/token")));

        DesktopUploadSelectedFile? selectedFile = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        Assert.NotNull(selectedFile);
        Assert.Equal(DesktopUploadSectionText.UnknownContentTypeValue, selectedFile.ContentType);
        Assert.DoesNotContain("Authorization", selectedFile.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", selectedFile.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignOutResetReturnsUploadStateToWorkspace()
    {
        var viewModel = new DesktopUploadSectionViewModel(new FakeDesktopVideoFilePicker());

        viewModel.OpenUploadSection();
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        viewModel.ResetForSignedOutState();

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
    }

    [Fact]
    public async Task BackToWorkspaceResetsSelectedFileState()
    {
        var viewModel = new DesktopUploadSectionViewModel(new FakeDesktopVideoFilePicker());

        viewModel.OpenUploadSection();
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);

        viewModel.BackToWorkspace();

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
    }

    [Fact]
    public void UploadVisualStringsDoNotExposeTokensPasswordAuthorizationOrRawSession()
    {
        string[] visibleStrings =
        [
            DesktopUploadSectionText.Title,
            DesktopUploadSectionText.Description,
            DesktopUploadSectionText.NavigationCardMessage,
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
            DesktopUploadSelectedFile.VisualSmokeFileName,
            DesktopUploadSelectedFile.VisualSmokeContentType
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