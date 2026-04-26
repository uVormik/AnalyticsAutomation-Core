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
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTechnicalIssueTypeLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldStatusLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldWarheadTypeLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDetonatorLabel, labels);
        Assert.Contains(global::App.Mobile.Android.Localization.MobileUiText.ReportFieldNsuLabel, labels);
    }

    [Fact]
    public async Task CreateFpvDraftAsync_CreatesExpectedFieldKeys()
    {
        var store = CreateStore();

        var draft = await store.CreateFpvDraftAsync();
        var keys = draft.Fields.Select(field => field.FieldKey).ToArray();

        Assert.Contains("device_type", keys);
        Assert.Contains("serial_number", keys);
        Assert.Contains("delivery_start", keys);
        Assert.Contains("delivery_time", keys);
        Assert.Contains("distance", keys);
        Assert.Contains("target_type", keys);
        Assert.Contains("reason", keys);
        Assert.Contains("comment", keys);
        Assert.Contains("radio_frequency", keys);
        Assert.Contains("video_frequency", keys);
        Assert.Contains("test_flight", keys);
        Assert.Contains("technical_issue_type", keys);
        Assert.Contains("status", keys);
        Assert.Contains("warhead_type", keys);
        Assert.Contains("detonator", keys);
        Assert.Contains("nsu", keys);
    }

    [Fact]
    public async Task CreateFpvDraftAsync_MarksExpectedLocalRequiredFieldsAsRequired()
    {
        var store = CreateStore();

        var draft = await store.CreateFpvDraftAsync();
        var requiredKeys = draft.Fields
            .Where(field => field.IsRequired)
            .Select(field => field.FieldKey)
            .OrderBy(fieldKey => fieldKey, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "delivery_start",
            "delivery_time",
            "device_type",
            "distance",
            "reason",
            "serial_number",
            "target_type"
        ],
        requiredKeys);
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

    [Fact]
    public async Task UpdateFieldValueAsync_UpdatesExistingField()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var result = await store.UpdateFieldValueAsync(draft.DraftId, "serial_number", "SN-001");

        Assert.True(result.Applied);
        Assert.NotNull(result.Draft);
        var field = Assert.Single(result.Draft!.Fields, item => item.FieldKey == "serial_number");
        Assert.Equal("SN-001", field.ValueText);
        Assert.False(field.IsPlaceholder);
        Assert.NotNull(field.LastUpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateFieldValueAsync_CanFillRequiredFields()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var result = await store.UpdateFieldValueAsync(draft.DraftId, "device_type", "Заглушка — значение 1");

        Assert.True(result.Applied);
        Assert.NotNull(result.Draft);
        var field = Assert.Single(result.Draft!.Fields, item => item.FieldKey == "device_type");
        Assert.False(field.IsPlaceholder);
        Assert.Equal("Заглушка — значение 1", field.ValueText);
    }

    [Fact]
    public async Task UpdateFieldValueAsync_MissingDraft_ReturnsAppliedFalse()
    {
        var store = CreateStore();

        var result = await store.UpdateFieldValueAsync("missing-draft", "serial_number", "SN-001");

        Assert.False(result.Applied);
        Assert.Null(result.Draft);
        Assert.Equal("serial_number", result.FieldKey);
    }

    [Fact]
    public async Task UpdateFieldValueAsync_MissingField_ReturnsAppliedFalse()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var result = await store.UpdateFieldValueAsync(draft.DraftId, "missing-field", "value");

        Assert.False(result.Applied);
        Assert.NotNull(result.Draft);
        Assert.Equal("missing-field", result.FieldKey);
    }

    [Fact]
    public async Task UpdatingRequiredFields_DoesNotRemoveAttachments()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();
        var attachResult = await store.AttachSelectedVideoAsync(draft.DraftId, CreateDescriptor());

        var updateResult = await store.UpdateFieldValueAsync(draft.DraftId, "device_type", "Заглушка — значение 1");

        Assert.True(attachResult.Applied);
        Assert.True(updateResult.Applied);
        Assert.NotNull(updateResult.Draft);
        Assert.Single(updateResult.Draft!.Attachments);
    }

    [Fact]
    public async Task MarkDraftQueuedLocal_StillWorksAfterValidationStyleFieldUpdate()
    {
        var store = CreateStore();
        var draft = await store.CreateFpvDraftAsync();

        var updateResult = await store.UpdateFieldValueAsync(draft.DraftId, "reason", "Заглушка — значение 2");
        var queueResult = await store.MarkDraftQueuedLocalAsync(draft.DraftId);

        Assert.True(updateResult.Applied);
        Assert.True(queueResult.Applied);
        Assert.NotNull(queueResult.Draft);
        Assert.Equal(global::App.Mobile.Android.Reports.MobileReportDraftStatus.QueuedLocal, queueResult.Draft!.Status);
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