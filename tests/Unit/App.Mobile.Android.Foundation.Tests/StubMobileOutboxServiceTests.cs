namespace App.Mobile.Android.Foundation.Tests;

public sealed class StubMobileOutboxServiceTests
{
    [Fact]
    public async Task EnqueueCurrentSelectionAsync_WithoutCurrentSelection_ReturnsAppliedFalse()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            var result = await context.Service.EnqueueCurrentSelectionAsync();

            Assert.False(result.Applied);
            Assert.Null(result.Item);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task EnqueueCurrentSelectionAsync_DuplicateCurrentSelection_ReturnsAppliedFalseAndKeepsCurrentSelection()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            await CacheCurrentSelectionAsync(context.SelectedMediaStore, cacheKey: "duplicate-cache-key");
            await context.Service.EnqueueCurrentSelectionAsync();
            await CacheCurrentSelectionAsync(context.SelectedMediaStore, cacheKey: "duplicate-cache-key");

            var result = await context.Service.EnqueueCurrentSelectionAsync();
            var items = await context.Service.GetItemsAsync();
            var currentSelection = await context.SelectedMediaStore.GetCurrentAsync();

            Assert.False(result.Applied);
            Assert.Single(items);
            Assert.NotNull(currentSelection);
            Assert.Equal("duplicate-cache-key", currentSelection!.CacheKey);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task EnqueueCurrentSelectionAsync_WithCachedSelection_CreatesOneQueuedItemWithLocalMediaDraft()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            await CacheCurrentSelectionAsync(context.SelectedMediaStore);

            var result = await context.Service.EnqueueCurrentSelectionAsync();
            var items = await context.Service.GetItemsAsync();
            var currentSelection = await context.SelectedMediaStore.GetCurrentAsync();

            Assert.True(result.Applied);
            Assert.Single(items);
            Assert.Equal(global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued, items[0].Status);
            Assert.NotNull(items[0].LocalMediaDraft);
            var localMediaDraft = items[0].LocalMediaDraft!;
            Assert.Equal("sample.mp4", localMediaDraft.FileName);
            Assert.True(localMediaDraft.HasLocalReadHandle);
            Assert.Null(currentSelection);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task PersistedQueueItems_LoadInNewServiceInstanceWithMetadataOnlyDraft()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var firstContext = CreateService(storageDirectory);
            await CacheCurrentSelectionAsync(firstContext.SelectedMediaStore);
            await firstContext.Service.EnqueueCurrentSelectionAsync();

            var secondContext = CreateService(storageDirectory);
            var items = await secondContext.Service.GetItemsAsync();

            Assert.Single(items);
            Assert.NotNull(items[0].LocalMediaDraft);
            Assert.False(items[0].LocalMediaDraft!.HasLocalReadHandle);
            Assert.Equal(
                global::App.Mobile.Android.Localization.MobileUiText.PendingSyncRestoredMetadataLastActionText,
                items[0].LastActionText);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task EnqueueCurrentSelectionAsync_WithRestoredMetadataOnlySelection_ReturnsAppliedFalse()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            await SeedSelectedMediaSnapshotOnlyAsync(storageDirectory);
            var context = CreateService(storageDirectory);
            var result = await context.Service.EnqueueCurrentSelectionAsync();
            var currentSelection = await context.SelectedMediaStore.GetCurrentAsync();
            var items = await context.Service.GetItemsAsync();

            Assert.False(result.Applied);
            Assert.NotNull(currentSelection);
            Assert.False(currentSelection!.HasLocalReadHandle);
            Assert.Empty(items);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RepairLocalMediaDraftAsync_WithoutCurrentSelection_ReturnsAppliedFalse()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            await SeedRestoredDraftMetadataAsync(storageDirectory);
            var context = CreateService(storageDirectory);
            var itemId = (await context.Service.GetItemsAsync())[0].ItemId;

            var result = await context.Service.RepairLocalMediaDraftAsync(itemId);
            var items = await context.Service.GetItemsAsync();

            Assert.False(result.Applied);
            Assert.False(items[0].LocalMediaDraft!.HasLocalReadHandle);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RepairLocalMediaDraftAsync_WithRestoredMetadataOnlyCurrentSelection_ReturnsAppliedFalse()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            await SeedSelectedMediaSnapshotOnlyAsync(storageDirectory);
            await SeedRestoredDraftMetadataAsync(storageDirectory);
            var context = CreateService(storageDirectory);
            var itemId = (await context.Service.GetItemsAsync())[0].ItemId;

            var result = await context.Service.RepairLocalMediaDraftAsync(itemId);
            var items = await context.Service.GetItemsAsync();

            Assert.False(result.Applied);
            Assert.False(items[0].LocalMediaDraft!.HasLocalReadHandle);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RepairLocalMediaDraftAsync_WithMatchingLiveCurrentSelection_ReturnsAppliedTrueAndClearsCurrentSelection()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            await SeedRestoredDraftMetadataAsync(storageDirectory);
            var context = CreateService(storageDirectory);
            var itemId = (await context.Service.GetItemsAsync())[0].ItemId;
            await CacheCurrentSelectionAsync(context.SelectedMediaStore, cacheKey: "restored-cache-key");

            var result = await context.Service.RepairLocalMediaDraftAsync(itemId);
            var items = await context.Service.GetItemsAsync();
            var currentSelection = await context.SelectedMediaStore.GetCurrentAsync();

            Assert.True(result.Applied);
            Assert.Single(items);
            Assert.True(items[0].LocalMediaDraft!.HasLocalReadHandle);
            Assert.Equal(
                global::App.Mobile.Android.Localization.MobileUiText.QueueRepairSuccessLastActionText,
                items[0].LastActionText);
            Assert.Null(currentSelection);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task SuccessfulRepair_SetsItemLocalMediaDraftHasLocalReadHandleTrue()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            await SeedRestoredDraftMetadataAsync(storageDirectory);
            var context = CreateService(storageDirectory);
            var itemId = (await context.Service.GetItemsAsync())[0].ItemId;
            await CacheCurrentSelectionAsync(context.SelectedMediaStore, cacheKey: "restored-cache-key");

            await context.Service.RepairLocalMediaDraftAsync(itemId);
            var items = await context.Service.GetItemsAsync();

            Assert.True(items[0].LocalMediaDraft!.HasLocalReadHandle);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RepairLocalMediaDraftAsync_WithMismatchedSelection_KeepsQueueItemMetadataOnly()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            await SeedRestoredDraftMetadataAsync(storageDirectory);
            var context = CreateService(storageDirectory);
            var itemId = (await context.Service.GetItemsAsync())[0].ItemId;
            await CacheCurrentSelectionAsync(
                context.SelectedMediaStore,
                cacheKey: "different-cache-key",
                fileName: "different.mp4",
                contentType: "video/mp4");

            var result = await context.Service.RepairLocalMediaDraftAsync(itemId);
            var items = await context.Service.GetItemsAsync();
            var currentSelection = await context.SelectedMediaStore.GetCurrentAsync();

            Assert.False(result.Applied);
            Assert.False(items[0].LocalMediaDraft!.HasLocalReadHandle);
            Assert.NotNull(currentSelection);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RetryAsync_StillUpdatesStatusAndLastActionText()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            await CacheCurrentSelectionAsync(context.SelectedMediaStore);
            var enqueueResult = await context.Service.EnqueueCurrentSelectionAsync();

            var retryResult = await context.Service.RetryAsync(enqueueResult.Item!.ItemId);
            var items = await context.Service.GetItemsAsync();

            Assert.True(retryResult.Applied);
            Assert.Single(items);
            Assert.Equal(global::App.Mobile.Android.Outbox.PendingSyncItemStatus.RetryRequested, items[0].Status);
            Assert.False(string.IsNullOrWhiteSpace(items[0].LastActionText));
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RemoveAsync_StillRemovesTheItem()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            await CacheCurrentSelectionAsync(context.SelectedMediaStore);
            var enqueueResult = await context.Service.EnqueueCurrentSelectionAsync();

            var removeResult = await context.Service.RemoveAsync(enqueueResult.Item!.ItemId);
            var items = await context.Service.GetItemsAsync();

            Assert.True(removeResult.Applied);
            Assert.Empty(items);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RemoveAsync_PersistsDeletion()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var firstContext = CreateService(storageDirectory);
            await CacheCurrentSelectionAsync(firstContext.SelectedMediaStore);
            var enqueueResult = await firstContext.Service.EnqueueCurrentSelectionAsync();

            var secondContext = CreateService(storageDirectory);
            await secondContext.Service.RemoveAsync(enqueueResult.Item!.ItemId);

            var thirdContext = CreateService(storageDirectory);
            var items = await thirdContext.Service.GetItemsAsync();

            Assert.Empty(items);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task EnqueueReportDraftAsync_WithNoVideoAttachments_ReturnsAppliedFalse()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            var result = await context.Service.EnqueueReportDraftAsync(CreateReportDraft(includeVideoAttachment: false));

            Assert.False(result.Applied);
            Assert.Null(result.Item);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task EnqueueReportDraftAsync_WithVideoAttachment_CreatesQueuedItemWithLocalReportDraft()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            var result = await context.Service.EnqueueReportDraftAsync(CreateReportDraft());
            var items = await context.Service.GetItemsAsync();

            Assert.True(result.Applied);
            Assert.Single(items);
            Assert.Equal(global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued, items[0].Status);
            Assert.NotNull(items[0].LocalReportDraft);
            Assert.Equal(1, items[0].LocalReportDraft!.VideoAttachmentCount);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task EnqueueReportDraftAsync_SameDraftTwice_ReturnsAppliedFalse()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            var draft = CreateReportDraft();

            var firstResult = await context.Service.EnqueueReportDraftAsync(draft);
            var secondResult = await context.Service.EnqueueReportDraftAsync(draft);
            var items = await context.Service.GetItemsAsync();

            Assert.True(firstResult.Applied);
            Assert.False(secondResult.Applied);
            Assert.Single(items);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RetryAsync_ReportDraftItem_StillWorks()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            var enqueueResult = await context.Service.EnqueueReportDraftAsync(CreateReportDraft());

            var retryResult = await context.Service.RetryAsync(enqueueResult.Item!.ItemId);
            var items = await context.Service.GetItemsAsync();

            Assert.True(retryResult.Applied);
            Assert.Single(items);
            Assert.Equal(global::App.Mobile.Android.Outbox.PendingSyncItemStatus.RetryRequested, items[0].Status);
            Assert.NotNull(items[0].LocalReportDraft);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task RemoveAsync_ReportDraftItem_StillRemovesTheItem()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var context = CreateService(storageDirectory);
            var enqueueResult = await context.Service.EnqueueReportDraftAsync(CreateReportDraft());

            var removeResult = await context.Service.RemoveAsync(enqueueResult.Item!.ItemId);
            var items = await context.Service.GetItemsAsync();

            Assert.True(removeResult.Applied);
            Assert.Empty(items);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    private static ServiceContext CreateService(string storageDirectory)
    {
        var selectedSnapshotStore = new global::App.Mobile.Android.Services.Local.FileMobileSelectedMediaSnapshotStore(
            GetSelectedMediaStorageDirectory(storageDirectory));
        var outboxSnapshotStore = new global::App.Mobile.Android.Services.Local.FileMobileOutboxSnapshotStore(
            GetOutboxStorageDirectory(storageDirectory));
        var selectedMediaStore = new global::App.Mobile.Android.Services.Local.InMemoryMobileSelectedMediaStore(selectedSnapshotStore);
        var duplicatePrecheckService = new global::App.Mobile.Android.Services.Local.LocalOutboxDuplicatePrecheckService();
        var repairService = new global::App.Mobile.Android.Services.Local.LocalCurrentSelectionDraftRepairService();

        return new ServiceContext(
            new global::App.Mobile.Android.Services.Stubs.StubMobileOutboxService(
                duplicatePrecheckService,
                repairService,
                selectedMediaStore,
                outboxSnapshotStore,
                global::Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    global::App.Mobile.Android.Services.Stubs.StubMobileOutboxService>.Instance),
            selectedMediaStore);
    }

    private static async Task SeedRestoredDraftMetadataAsync(string storageDirectory)
    {
        var outboxSnapshotStore = new global::App.Mobile.Android.Services.Local.FileMobileOutboxSnapshotStore(
            GetOutboxStorageDirectory(storageDirectory));
        await outboxSnapshotStore.SaveAsync(
        [
            new global::App.Mobile.Android.Outbox.PendingSyncItem(
                ItemId: "pending-sync-1",
                CreatedAtUtc: new DateTimeOffset(2026, 4, 18, 9, 0, 0, TimeSpan.Zero),
                Title: "Draft 1",
                SummaryText: "Local draft",
                Status: global::App.Mobile.Android.Outbox.PendingSyncItemStatus.Queued,
                LastActionText: global::App.Mobile.Android.Localization.MobileUiText.PendingSyncRestoredMetadataLastActionText,
                LocalMediaDraft: new global::App.Mobile.Android.Outbox.PendingSyncItemLocalMediaDraft(
                    CacheKey: "restored-cache-key",
                    Source: global::App.Mobile.Android.Media.MobileMediaSource.GalleryVideo,
                    FileName: "sample.mp4",
                    ContentType: "video/mp4",
                    SelectedAtUtc: new DateTimeOffset(2026, 4, 18, 8, 55, 0, TimeSpan.Zero),
                    HasLocalReadHandle: false))
        ]);
    }

    private static async Task SeedSelectedMediaSnapshotOnlyAsync(string storageDirectory)
    {
        var selectedSnapshotStore = new global::App.Mobile.Android.Services.Local.FileMobileSelectedMediaSnapshotStore(
            GetSelectedMediaStorageDirectory(storageDirectory));
        await selectedSnapshotStore.SaveAsync(
            new global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor(
                CacheKey: "restored-cache-key",
                Source: global::App.Mobile.Android.Media.MobileMediaSource.GalleryVideo,
                FileName: "sample.mp4",
                ContentType: "video/mp4",
                SelectedAtUtc: new DateTimeOffset(2026, 4, 18, 8, 55, 0, TimeSpan.Zero),
                HasLocalReadHandle: false));
    }

    private static Task CacheCurrentSelectionAsync(
        global::App.Mobile.Android.Services.Local.InMemoryMobileSelectedMediaStore selectedMediaStore,
        string cacheKey = "selected-media-cache-key",
        string fileName = "sample.mp4",
        string? contentType = "video/mp4")
    {
        return selectedMediaStore.CacheAsync(
            new global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor(
                CacheKey: cacheKey,
                Source: global::App.Mobile.Android.Media.MobileMediaSource.GalleryVideo,
                FileName: fileName,
                ContentType: contentType,
                SelectedAtUtc: new DateTimeOffset(2026, 4, 16, 12, 0, 0, TimeSpan.Zero),
                HasLocalReadHandle: true),
            () => Task.FromResult<global::System.IO.Stream>(
                new global::System.IO.MemoryStream([1, 2, 3, 4])));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"androida-outbox-service-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string GetSelectedMediaStorageDirectory(string storageDirectory)
    {
        return Path.Combine(storageDirectory, "selected");
    }

    private static string GetOutboxStorageDirectory(string storageDirectory)
    {
        return Path.Combine(storageDirectory, "outbox");
    }

    private static global::App.Mobile.Android.Reports.MobileReportDraft CreateReportDraft(bool includeVideoAttachment = true)
    {
        var attachments = includeVideoAttachment
            ? new[]
            {
                new global::App.Mobile.Android.Reports.MobileReportAttachment(
                    AttachmentId: "attachment-1",
                    DraftId: "draft-1",
                    Kind: global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video,
                    FileName: "report-video.mp4",
                    ContentType: "video/mp4",
                    SourceText: "Галерея",
                    AddedAtUtc: new DateTimeOffset(2026, 4, 21, 9, 0, 0, TimeSpan.Zero),
                    SelectedMediaCacheKey: "report-cache-key",
                    HasLocalReadHandle: true)
            }
            : Array.Empty<global::App.Mobile.Android.Reports.MobileReportAttachment>();

        return new global::App.Mobile.Android.Reports.MobileReportDraft(
            DraftId: "draft-1",
            CreatedAtUtc: new DateTimeOffset(2026, 4, 21, 8, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc: new DateTimeOffset(2026, 4, 21, 8, 30, 0, TimeSpan.Zero),
            Title: "FPV-отчет #1",
            Status: global::App.Mobile.Android.Reports.MobileReportDraftStatus.ReadyForAttachmentReview,
            Fields:
            [
                new global::App.Mobile.Android.Reports.MobileReportDraftFieldValue(
                    FieldKey: "device_type",
                    Label: "Тип дрона",
                    ValueText: "Локальная заглушка",
                    IsRequired: true,
                    IsPlaceholder: true)
            ],
            Attachments: attachments);
    }

    private sealed record ServiceContext(
        global::App.Mobile.Android.Services.Stubs.StubMobileOutboxService Service,
        global::App.Mobile.Android.Services.Local.InMemoryMobileSelectedMediaStore SelectedMediaStore);
}