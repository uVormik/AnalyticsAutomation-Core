using System.Net;
using System.Text;
using System.Text.Json;

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
    public void LiveGroupTreeRequiresConfiguredControlPlaneBaseAddress()
    {
        DesktopGroupTreeOptions disabled = DesktopGroupTreeOptions.FromEnvironmentValues(
            controlPlaneBaseAddress: null,
            devFakeGroupTreeEnabled: null);
        DesktopGroupTreeOptions live = DesktopGroupTreeOptions.FromEnvironmentValues(
            "https://control-plane.local",
            devFakeGroupTreeEnabled: null);

        Assert.False(disabled.IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(disabled.IsGroupTreeClientConfigured);
        Assert.True(live.IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(live.IsDevFakeGroupTreeEnabled);
        Assert.True(live.IsGroupTreeClientConfigured);
    }

    [Fact]
    public void LiveBaseAddressDisablesFakeGroupTreeSelection()
    {
        DesktopGroupTreeOptions options = DesktopGroupTreeOptions.FromEnvironmentValues(
            "https://control-plane.local",
            devFakeGroupTreeEnabled: "true");

        Assert.True(options.IsLiveControlPlaneGroupTreeEnabled);
        Assert.False(options.IsDevFakeGroupTreeEnabled);
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
    public async Task HttpGroupTreeClientUsesExistingNodesEndpointAndReturnsSafeNodes()
    {
        string accessToken = CreateSensitiveValue("access");
        Guid rootId = Guid.NewGuid();
        Guid branchId = Guid.NewGuid();
        Guid siteId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent<object[]>(
            [
                new
                {
                    groupNodeId = rootId,
                    parentGroupNodeId = (Guid?)null,
                    code = "root",
                    name = "Вся организация",
                    depth = 0,
                    isActive = true
                },
                new
                {
                    groupNodeId = branchId,
                    parentGroupNodeId = (Guid?)rootId,
                    code = "branch-001",
                    name = "Тестовая ветка",
                    depth = 1,
                    isActive = true
                },
                new
                {
                    groupNodeId = siteId,
                    parentGroupNodeId = (Guid?)branchId,
                    code = "site-001",
                    name = "Тестовый объект",
                    depth = 2,
                    isActive = true
                }
            ])
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://control-plane.local")
        };
        var client = new HttpDesktopGroupTreeClient(
            httpClient,
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken));

        DesktopGroupTreeLoadResult result = await client.GetGroupTreeAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Loaded, result.Status);
        Assert.Equal(DesktopGroupTreeText.LiveLoadedMessage, result.Message);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal("/api/group-tree/nodes", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal(accessToken, handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Collection(
            result.Nodes,
            node => AssertNode(node, rootId.ToString("D"), "Вся организация", canSelect: false),
            node => AssertNode(node, branchId.ToString("D"), "Тестовая ветка", canSelect: false),
            node => AssertNode(node, siteId.ToString("D"), "Тестовый объект", canSelect: true));
        Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpGroupTreeClientDoesNotExposeTokenValuesOrRawFailureBody()
    {
        string accessToken = CreateSensitiveValue("access");
        string responseToken = CreateSensitiveValue("refresh");
        string rawBody = $"{{\"accessToken\":\"{responseToken}\",\"detail\":\"unsafe\"}}";
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://control-plane.local")
        };
        var client = new HttpDesktopGroupTreeClient(
            httpClient,
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken));

        DesktopGroupTreeLoadResult result = await client.GetGroupTreeAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Failed, result.Status);
        Assert.Equal(DesktopGroupTreeText.LiveFailedMessage, result.Message);
        Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(responseToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(rawBody, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpGroupTreeClientHandlesUnavailableAndMalformedResponsesSafely()
    {
        string accessToken = CreateSensitiveValue("access");
        var unavailableHandler = new StubHttpMessageHandler(_ => throw new HttpRequestException(
            $"Transport failed with {accessToken}"));
        var unavailableClient = new HttpDesktopGroupTreeClient(
            new HttpClient(unavailableHandler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken));

        DesktopGroupTreeLoadResult unavailable =
            await unavailableClient.GetGroupTreeAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Unavailable, unavailable.Status);
        Assert.Equal(DesktopGroupTreeText.LiveUnavailableMessage, unavailable.Message);
        Assert.DoesNotContain(accessToken, unavailable.ToString(), StringComparison.Ordinal);

        string malformedBody = $"{{\"refreshToken\":\"{CreateSensitiveValue("refresh")}\",";
        var malformedHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(malformedBody, Encoding.UTF8, "application/json")
        });
        var malformedClient = new HttpDesktopGroupTreeClient(
            new HttpClient(malformedHandler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken));

        DesktopGroupTreeLoadResult malformed =
            await malformedClient.GetGroupTreeAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Failed, malformed.Status);
        Assert.Equal(DesktopGroupTreeText.LiveMalformedMessage, malformed.Message);
        Assert.DoesNotContain(accessToken, malformed.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(malformedBody, malformed.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpGroupTreeClientWithoutSessionShowsSafeUnavailableMessage()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(Array.Empty<object>())
        });
        var client = new HttpDesktopGroupTreeClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://control-plane.local")
            },
            new StaticDesktopControlPlaneAccessTokenProvider(accessToken: null));

        DesktopGroupTreeLoadResult result = await client.GetGroupTreeAsync(CancellationToken.None);

        Assert.Equal(DesktopGroupTreeLoadStatus.Unavailable, result.Status);
        Assert.Equal(DesktopGroupTreeText.LiveUnauthorizedMessage, result.Message);
        Assert.Null(handler.LastRequest);
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
    public void GroupTreeFilesOnlyUseApprovedGroupTreeEndpointAndNoUploadChainCalls()
    {
        string[] sourceFiles =
        [
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Boundaries", "DesktopGroupTreeBoundary.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DesktopGroupTreeOptions.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DesktopGroupSelectionState.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DesktopGroupTreeViewModel.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "DisabledDesktopGroupTreeClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "FakeDesktopGroupTreeClient.cs"),
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "HttpDesktopGroupTreeClient.cs")
        ];

        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);

            Assert.DoesNotContain("IControlPlaneApiClient", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RequestPreUploadCheckAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("RecordUploadReceiptAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PostAsync", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/api/group-tree/routing-preview", source, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/auth", source, StringComparison.Ordinal);
        }

        string liveClientSource = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "src", "App.Desktop", "Services", "GroupTree", "HttpDesktopGroupTreeClient.cs"));

        Assert.Contains("/api/group-tree/nodes", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/group-tree/nodes/", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadAsStringAsync", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteLine", liveClientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Log", liveClientSource, StringComparison.Ordinal);
    }

    [Fact]
    public void LiveGroupTreeVisualStringsDoNotExposeTokenLabels()
    {
        string[] visibleStrings =
        [
            DesktopGroupTreeText.LiveLoadingMessage,
            DesktopGroupTreeText.LiveLoadedMessage,
            DesktopGroupTreeText.LiveUnauthorizedMessage,
            DesktopGroupTreeText.LiveUnavailableMessage,
            DesktopGroupTreeText.LiveFailedMessage,
            DesktopGroupTreeText.LiveMalformedMessage,
            DesktopGroupTreeLoadResult.Unavailable(DesktopGroupTreeText.LiveUnauthorizedMessage).ToString(),
            DesktopGroupTreeLoadResult.Failed(DesktopGroupTreeText.LiveMalformedMessage).ToString()
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

    private sealed class StaticDesktopControlPlaneAccessTokenProvider(string? accessToken) :
        IDesktopControlPlaneAccessTokenProvider
    {
        public ValueTask<DesktopControlPlaneAccessTokenSnapshot> GetCurrentAccessTokenAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new DesktopControlPlaneAccessTokenSnapshot(
                IsAuthenticated: !string.IsNullOrWhiteSpace(accessToken),
                accessToken));
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) :
        HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }

    private static StringContent JsonContent<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value),
            Encoding.UTF8,
            "application/json");
    }

    private static string CreateSensitiveValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
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