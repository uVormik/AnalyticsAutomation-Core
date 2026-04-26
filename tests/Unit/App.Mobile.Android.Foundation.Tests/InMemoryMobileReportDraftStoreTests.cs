namespace App.Mobile.Android.Foundation.Tests;

public sealed class InMemoryMobileReportDraftStoreTests
{
    [Fact]
    public async Task CreateFpvDraftAsync_CreatesOneDraftWithDraftStatus()
    {
        var store = CreateStore();

        var draft = await store.CreateFpvDraftAsync();

        Assert.Equal(global::App.Mobile.Android.Reports.MobileReportDraftStatus.Draft, draft.Status);
        Assert.NotEmpty(draft.DraftId);
    }

    [Fact]
    public async Task CreatedDraft_ContainsObservedPlaceholderFieldLabels()
    {
        var store = CreateStore();

        var draft = await store.CreateFpvDraftAsync();
        var labels = draft.Fields.Select(field => field.Label).ToArray();

        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeviceTypeLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldSerialNumberLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeliveryStartLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeliveryTimeLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDistanceLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTargetTypeLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldReasonLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldCommentLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldRadioFrequencyLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldVideoFrequencyLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTestFlightLabel, labels);
    }

    [Fact]
    public async Task GetDraftsAsync_ReturnsCreatedDraft()
    {
        var store = CreateStore();
        var createdDraft = await store.CreateFpvDraftAsync();

        var drafts = await store.GetDraftsAsync();

        Assert.Single(drafts);
        Assert.Equal(createdDraft.DraftId, drafts[0].DraftId);
    }

    [Fact]
    public async Task GetDraftAsync_ReturnsCreatedDraft()
    {
        var store = CreateStore();
        var createdDraft = await store.CreateFpvDraftAsync();

        var draft = await store.GetDraftAsync(createdDraft.DraftId);

        Assert.NotNull(draft);
        Assert.Equal(createdDraft.DraftId, draft!.DraftId);
    }

    [Fact]
    public async Task AttachSelectedVideoAsync_AddsOneVideoAttachment()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var result = await store.AttachSelectedVideoAsync(draft.DraftId, CreateDescriptor());

        Assert.True(result.Applied);
        Assert.NotNull(result.Draft);
        Assert.NotNull(result.Attachment);
        Assert.Equal(global::App.Mobile.Android.Reports.MobileReportDraftStatus.ReadyForAttachmentReview, result.Draft!.Status);
        Assert.Single(result.Draft.Attachments);
        Assert.Equal(global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video, result.Draft.Attachments[0].Kind);
    }

    [Fact]
    public async Task AttachSelectedVideoAsync_SameCacheKeyTwice_ReturnsAppliedFalseAndKeepsOneAttachment()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();
        var descriptor = CreateDescriptor(cacheKey: "duplicate-cache-key");

        var firstResult = await store.AttachSelectedVideoAsync(draft.DraftId, descriptor);
        var secondResult = await store.AttachSelectedVideoAsync(draft.DraftId, descriptor);

        Assert.True(firstResult.Applied);
        Assert.False(secondResult.Applied);
        Assert.NotNull(secondResult.Draft);
        Assert.Single(secondResult.Draft!.Attachments);
    }

    [Fact]
    public async Task AttachSelectedVideoAsync_SameFileNameAndContentTypeTwice_ReturnsAppliedFalseAndKeepsOneAttachment()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var firstResult = await store.AttachSelectedVideoAsync(
            draft.DraftId,
            CreateDescriptor(cacheKey: "cache-a", fileName: "same.mp4", contentType: "video/mp4"));
        var secondResult = await store.AttachSelectedVideoAsync(
            draft.DraftId,
            CreateDescriptor(cacheKey: "cache-b", fileName: "SAME.mp4", contentType: " video/mp4 "));

        Assert.True(firstResult.Applied);
        Assert.False(secondResult.Applied);
        Assert.NotNull(secondResult.Draft);
        Assert.Single(secondResult.Draft!.Attachments);
    }

    [Fact]
    public async Task AttachSelectedVideoAsync_DifferentVideo_Succeeds()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var firstResult = await store.AttachSelectedVideoAsync(
            draft.DraftId,
            CreateDescriptor(cacheKey: "cache-a", fileName: "first.mp4"));
        var secondResult = await store.AttachSelectedVideoAsync(
            draft.DraftId,
            CreateDescriptor(cacheKey: "cache-b", fileName: "second.mp4"));

        Assert.True(firstResult.Applied);
        Assert.True(secondResult.Applied);
        Assert.NotNull(secondResult.Draft);
        Assert.Equal(2, secondResult.Draft!.Attachments.Count);
    }

    [Fact]
    public async Task MarkDraftQueuedLocalAsync_SetsStatusQueuedLocal()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var result = await store.MarkDraftQueuedLocalAsync(draft.DraftId);

        Assert.True(result.Applied);
        Assert.NotNull(result.Draft);
        Assert.Equal(global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal, result.Draft!.Status);
    }

    [Fact]
    public async Task AttachSelectedVideoAsync_DoesNotRequireBackendFieldsOrBusinessObjectKey()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var result = await store.AttachSelectedVideoAsync(draft.DraftId, CreateDescriptor());

        Assert.True(result.Applied);
        Assert.NotNull(result.Draft);
        Assert.DoesNotContain(result.Draft!.Fields, field =>
            string.Equals(field.FieldKey, "businessObjectKey", StringComparison.OrdinalIgnoreCase));
    }

    private static global::App.Mobile.Android.Services.Local.InMemoryMobileReportDraftStore CreateStore()
    {
        return new global::App.Mobile.Android.Services.Local.InMemoryMobileReportDraftStore(
            new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider());
    }

    private static global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor CreateDescriptor(
        string cacheKey = "cache-video-001",
        string fileName = "fpv-video.mp4",
        string? contentType = "video/mp4")
    {
        return new global::App.Mobile.Android.Media.LocalSelectedMediaDescriptor(
            CacheKey: cacheKey,
            Source: global::App.Mobile.Android.Media.MobileMediaSource.GalleryVideo,
            FileName: fileName,
            ContentType: contentType,
            SelectedAtUtc: DateTimeOffset.UtcNow,
            HasLocalReadHandle: true);
    }
}