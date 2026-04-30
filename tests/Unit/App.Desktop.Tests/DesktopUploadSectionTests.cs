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
        var viewModel = new DesktopUploadSectionViewModel(DesktopUploadSectionOptions.Disabled);

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
            "Этот раздел подготовлен. Реальная загрузка будет включена в следующем approved desktop slice.",
            DesktopUploadSectionText.Description);
        Assert.Equal("Шаг 1. Выбор видеофайла", DesktopUploadSectionText.StepOneTitle);
        Assert.Equal("Выбрать видеофайл", DesktopUploadSectionText.SelectVideoFileButton);
        Assert.Equal(
            "Выбор файла пока работает в режиме заготовки.",
            DesktopUploadSectionText.PlaceholderResult);
        Assert.Equal("Назад к рабочей области", DesktopUploadSectionText.BackToWorkspaceButton);
    }

    [Fact]
    public void UploadSectionDoesNotReferenceApiFilePickerHashingOrUploadFlowBoundaries()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Components", "DesktopShell.razor"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionViewModel.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionOptions.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopSignInBoundary.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("App.Api", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IControlPlaneApiClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IDesktopUploadOrchestrator", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IDesktopFilePicker", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadSha256", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SHA-256", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("businessObjectKey", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PreUploadCheck", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DirectSiteUpload", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UploadReceipt", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FilePath", source, StringComparison.Ordinal);
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
    public void FakeUploadFileSelectionDoesNothingWhenDisabled()
    {
        var viewModel = new DesktopUploadSectionViewModel(DesktopUploadSectionOptions.Disabled);

        DesktopUploadSelectedFile? selectedFile = viewModel.SelectVideoFilePlaceholder();

        Assert.Null(selectedFile);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
    }

    [Fact]
    public void FakeSelectedFileStateIsSafeAndContainsNoRealPathOrSecret()
    {
        var viewModel = new DesktopUploadSectionViewModel(
            DesktopUploadSectionOptions.EnabledForDevFakeFileSelection);

        DesktopUploadSelectedFile? selectedFile = viewModel.SelectVideoFilePlaceholder();

        Assert.NotNull(selectedFile);
        Assert.Equal("visual-smoke-video.mp4", selectedFile.FileName);
        Assert.Equal(12345678, selectedFile.SizeBytes);
        Assert.Equal("video/mp4", selectedFile.ContentType);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);

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
    public void SignOutResetReturnsUploadStateToWorkspace()
    {
        var viewModel = new DesktopUploadSectionViewModel(
            DesktopUploadSectionOptions.EnabledForDevFakeFileSelection);

        viewModel.OpenUploadSection();
        _ = viewModel.SelectVideoFilePlaceholder();

        viewModel.ResetForSignedOutState();

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
            DesktopUploadSectionText.BackToWorkspaceButton,
            DesktopUploadSectionText.SelectedFileNameLabel,
            DesktopUploadSectionText.SelectedFileSizeLabel,
            DesktopUploadSectionText.SelectedFileContentTypeLabel,
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