using App.Desktop.Boundaries;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

namespace App.Desktop.Tests;

public sealed class DesktopGroupTreeTests
{
    [Fact]
    public void FakeGroupTreeIsDisabledByDefault()
    {
        Assert.False(DesktopGroupTreeOptions.Disabled.IsDevFakeGroupTreeEnabled);
        Assert.False(DesktopGroupTreeOptions.FromEnvironmentValue(null).IsDevFakeGroupTreeEnabled);
        Assert.False(DesktopGroupTreeOptions.FromEnvironmentValue("false").IsDevFakeGroupTreeEnabled);
        Assert.False(DesktopGroupTreeOptions.FromEnvironmentValue("1").IsDevFakeGroupTreeEnabled);
    }

    [Theory]
    [InlineData("true")]
    [InlineData(" TRUE ")]
    public void FakeGroupTreeIsEnabledOnlyByExplicitTrueValue(string value)
    {
        DesktopGroupTreeOptions options = DesktopGroupTreeOptions.FromEnvironmentValue(value);

#if DEBUG
        Assert.True(options.IsDevFakeGroupTreeEnabled);
#else
        Assert.False(options.IsDevFakeGroupTreeEnabled);
#endif
    }

    [Fact]
    public async Task DisabledGroupTreeShowsSafeMessageAndDoesNotCallBoundary()
    {
        var client = new ThrowingDesktopGroupTreeClient();
        var viewModel = new DesktopGroupTreeViewModel(
            client,
            DesktopGroupTreeOptions.Disabled,
            new DesktopGroupSelectionState());

        DesktopGroupTreeLoadResult result = await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Disabled, result.Status);
        Assert.Empty(viewModel.Nodes);
        Assert.Equal(DesktopGroupTreeText.DisabledMessage, viewModel.StatusMessage);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task FakeGroupTreeNodesUseSafeRussianLabels()
    {
        DesktopGroupTreeLoadResult result =
            await new FakeDesktopGroupTreeClient().GetGroupTreeAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Loaded, result.Status);
        Assert.Collection(
            result.Nodes,
            node => AssertNode(node, "root", "Вся организация", canSelect: false),
            node => AssertNode(node, "branch-001", "Тестовая ветка", canSelect: false),
            node => AssertNode(node, "site-001", "Тестовый объект", canSelect: true));
    }

    [Fact]
    public async Task SelectingFakeSiteGroupShowsSafeSelectedPreview()
    {
        var viewModel = CreateEnabledViewModel();
        _ = await viewModel.LoadAsync(CancellationToken.None);

        DesktopSelectedGroupContext? selected = viewModel.SelectGroup("site-001");

#if DEBUG
        Assert.NotNull(selected);
        Assert.Equal("site-001", selected.Id);
        Assert.Equal("Тестовый объект", selected.DisplayName);
        Assert.Equal("Выбрана группа: Тестовый объект", viewModel.SelectedGroupMessage);
        Assert.Equal(DesktopGroupTreeText.LoadedMessage, viewModel.StatusMessage);
#else
        Assert.Null(selected);
        Assert.False(viewModel.HasSelectedGroup);
#endif
    }

    [Fact]
    public void UploadSectionShowsSafeMissingGroupContextMessageWhenNotSelected()
    {
        var viewModel = CreateUploadViewModel(new DesktopGroupSelectionState());

        Assert.False(viewModel.HasSelectedGroupContext);
        Assert.Equal(DesktopUploadSectionText.GroupContextTitle, "Контекст группы");
        Assert.Equal(
            "Группа не выбрана. Для production-flow выбор группы будет обязательным в отдельном slice.",
            viewModel.GroupContextStatusMessage);
    }

    [Fact]
    public void UploadSectionShowsSelectedGroupContextWhenSelected()
    {
        var groupSelectionState = new DesktopGroupSelectionState();
        Assert.True(groupSelectionState.TrySelect(DesktopGroupTreeNode.VisualSmokeNodes[2]));
        var viewModel = CreateUploadViewModel(groupSelectionState);

        Assert.True(viewModel.HasSelectedGroupContext);
        Assert.Equal("Тестовый объект", viewModel.SelectedGroupDisplayName);
        Assert.Equal("site-001", viewModel.SelectedGroupId);
        Assert.Equal(string.Empty, viewModel.GroupContextStatusMessage);
    }

    [Fact]
    public async Task SelectedGroupContextDoesNotChangeExistingFakeUploadChainPreviews()
    {
        var groupSelectionState = new DesktopGroupSelectionState();
        Assert.True(groupSelectionState.TrySelect(DesktopGroupTreeNode.VisualSmokeNodes[2]));
        var viewModel = new DesktopUploadSectionViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new FakeDesktopPreUploadCheckClient(),
            new FakeDesktopDirectSiteUploadClient(),
            new FakeDesktopUploadReceiptClient(),
            groupSelectionState,
            DesktopUploadSectionOptions.FromEnvironmentValue("false", "false", "false", "true", "true", "true"));

        _ = await viewModel.SelectVideoFileAsync(CancellationToken.None);
        _ = await viewModel.CalculateSha256Async(CancellationToken.None);
        viewModel.BusinessObjectKeyInput = "report-draft-001";
        _ = viewModel.ApplyBusinessObjectKey();

        DesktopPreUploadCheckRequestPreview? preview = viewModel.PreUploadCheckRequestPreview;

        Assert.NotNull(preview);
        Assert.Equal("report-draft-001", preview.BusinessObjectKeyPreview);
        Assert.DoesNotContain("site-001", preview.ToString(), StringComparison.Ordinal);

        _ = await viewModel.CheckPreUploadAsync(CancellationToken.None);
        _ = await viewModel.UploadToSiteAsync(CancellationToken.None);
        _ = await viewModel.CreateUploadReceiptAsync(CancellationToken.None);

#if DEBUG
        Assert.Equal("ALLOW", viewModel.PreUploadCheckDecisionPreview);
        Assert.Equal("SUCCESS", viewModel.SiteUploadStatusPreview);
        Assert.Equal("ACCEPTED", viewModel.UploadReceiptStatusPreview);
#else
        Assert.Null(viewModel.PreUploadCheckDecisionPreview);
        Assert.Null(viewModel.SiteUploadStatusPreview);
        Assert.Null(viewModel.UploadReceiptStatusPreview);
#endif
    }

    [Fact]
    public void BackAndSignOutClearSelectedGroupState()
    {
        var groupSelectionState = new DesktopGroupSelectionState();
        Assert.True(groupSelectionState.TrySelect(DesktopGroupTreeNode.VisualSmokeNodes[2]));
        var viewModel = CreateUploadViewModel(groupSelectionState);

        viewModel.BackToWorkspace();

        Assert.Null(groupSelectionState.SelectedGroup);
        Assert.False(viewModel.HasSelectedGroupContext);

        Assert.True(groupSelectionState.TrySelect(DesktopGroupTreeNode.VisualSmokeNodes[2]));

        viewModel.ResetForSignedOutState();

        Assert.Null(groupSelectionState.SelectedGroup);
        Assert.False(viewModel.HasSelectedGroupContext);
    }

    [Fact]
    public void GroupTreeFilesDoNotIntroduceRealAppApiCalls()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopGroupTreeBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DesktopGroupTreeOptions.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DesktopGroupSelectionState.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DesktopGroupTreeViewModel.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DisabledDesktopGroupTreeClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "FakeDesktopGroupTreeClient.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IControlPlaneApiClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RequestPreUploadCheckAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RecordUploadReceiptAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("GetAsync", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PostAsync", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SendAsync", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GroupTreeAndGroupContextVisualStringsDoNotExposeSecretsOrFullLocalPath()
    {
        var groupSelectionState = new DesktopGroupSelectionState();
        Assert.True(groupSelectionState.TrySelect(DesktopGroupTreeNode.VisualSmokeNodes[2]));
        string[] visibleStrings =
        [
            DesktopGroupTreeText.Title,
            DesktopGroupTreeText.Description,
            DesktopGroupTreeText.NavigationCardMessage,
            DesktopGroupTreeText.DisabledMessage,
            DesktopGroupTreeText.LoadingMessage,
            DesktopGroupTreeText.LoadedMessage,
            DesktopGroupTreeText.SelectionUnavailableMessage,
            DesktopGroupTreeText.BackToWorkspaceButton,
            DesktopGroupTreeText.SelectGroupButton,
            DesktopGroupTreeText.ContinueToUploadButton,
            DesktopGroupTreeText.GroupIdLabel,
            DesktopGroupTreeText.CreateSelectedGroupMessage("Тестовый объект"),
            DesktopGroupTreeNode.RootId,
            DesktopGroupTreeNode.RootDisplayName,
            DesktopGroupTreeNode.BranchId,
            DesktopGroupTreeNode.BranchDisplayName,
            DesktopGroupTreeNode.SiteId,
            DesktopGroupTreeNode.SiteDisplayName,
            groupSelectionState.SelectedGroup?.ToString() ?? string.Empty,
            DesktopUploadSectionText.GroupContextTitle,
            DesktopUploadSectionText.GroupContextMissingMessage,
            DesktopUploadSectionText.GroupContextNameLabel,
            DesktopUploadSectionText.GroupContextIdLabel
        ];

        foreach (string visibleString in visibleStrings)
        {
            Assert.DoesNotContain("password", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authorization", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refreshToken", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("session_id", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("raw response", visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.GetTempPath(), visibleString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"\", visibleString, StringComparison.Ordinal);
        }
    }

    private static DesktopGroupTreeViewModel CreateEnabledViewModel()
    {
        return new DesktopGroupTreeViewModel(
            new FakeDesktopGroupTreeClient(),
            DesktopGroupTreeOptions.EnabledForDevFakeGroupTree,
            new DesktopGroupSelectionState());
    }

    private static DesktopUploadSectionViewModel CreateUploadViewModel(
        DesktopGroupSelectionState groupSelectionState)
    {
        return new DesktopUploadSectionViewModel(
            new FakeDesktopVideoFilePicker(),
            new FakeDesktopVideoHashService(),
            new DisabledDesktopPreUploadCheckClient(),
            new DisabledDesktopDirectSiteUploadClient(),
            new DisabledDesktopUploadReceiptClient(),
            groupSelectionState,
            DesktopUploadSectionOptions.Disabled);
    }

    private static void AssertNode(
        DesktopGroupTreeNode node,
        string expectedId,
        string expectedDisplayName,
        bool canSelect)
    {
        Assert.Equal(expectedId, node.Id);
        Assert.Equal(expectedDisplayName, node.DisplayName);
        Assert.Equal(canSelect, node.CanSelect);
        Assert.DoesNotContain("password", node.Preview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", node.Preview, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingDesktopGroupTreeClient : IDesktopGroupTreeClient
    {
        public int CallCount { get; private set; }

        public ValueTask<DesktopGroupTreeLoadResult> GetGroupTreeAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("Disabled fake GroupTree boundary should not be called.");
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