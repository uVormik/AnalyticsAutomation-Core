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
    }

    [Fact]
    public void UploadSectionDoesNotReferenceApiOrUploadFlowBoundaries()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Components", "DesktopShell.razor"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionViewModel.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopUploadSectionOptions.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopVideoFilePicker.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "FakeDesktopVideoHashService.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "DesktopVideoHashService.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "Upload", "WpfDesktopVideoFilePicker.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopVideoFilePickerBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopVideoHashBoundary.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("App.Api", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IControlPlaneApiClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IDesktopUploadOrchestrator", source, StringComparison.Ordinal);
            Assert.DoesNotContain("businessObjectKey", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PreUploadCheck", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DirectSiteUpload", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UploadReceipt", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ReadAllBytes", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FakeUploadFileSelectionAndHashAreDevOnlyAndDisabledByDefault()
    {
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.Disabled.IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue(null).IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false").IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "false").IsDevFakeUploadHashEnabled);

#if DEBUG
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true").IsDevFakeUploadFileEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("false", "true").IsDevFakeUploadHashEnabled);
        Assert.True(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true").IsDevFakeUploadHashEnabled);
#else
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true").IsDevFakeUploadFileEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("false", "true").IsDevFakeUploadHashEnabled);
        Assert.False(DesktopUploadSectionOptions.FromEnvironmentValue("true", "true").IsDevFakeUploadHashEnabled);
#endif
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
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        viewModel.OpenUploadSection();
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);

        viewModel.ResetForSignedOutState();

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
        Assert.Equal(DesktopUploadSectionText.HashNotReadyMessage, viewModel.HashStatusMessage);
        Assert.Null(viewModel.Sha256Hex);
        Assert.False(viewModel.CanCalculateHash);
    }

    [Fact]
    public async Task BackToWorkspaceResetsSelectedFileState()
    {
        var viewModel = CreateViewModel(new FakeDesktopVideoFilePicker());

        viewModel.OpenUploadSection();
        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);

        viewModel.BackToWorkspace();

        Assert.Equal(DesktopWorkspaceSection.Workspace, viewModel.CurrentSection);
        Assert.False(viewModel.IsUploadSectionOpen);
        Assert.Null(viewModel.SelectedFile);
        Assert.Equal(DesktopUploadSectionText.PlaceholderResult, viewModel.SelectionStatusMessage);
        Assert.Equal(DesktopUploadSectionText.HashNotReadyMessage, viewModel.HashStatusMessage);
        Assert.Null(viewModel.Sha256Hex);
        Assert.False(viewModel.CanCalculateHash);
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
            DesktopUploadSelectedFile.VisualSmokeFileName,
            DesktopUploadSelectedFile.VisualSmokeContentType,
            FakeDesktopVideoHashService.VisualSmokeSha256Hex
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
        return new DesktopUploadSectionViewModel(videoFilePicker, videoHashService);
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