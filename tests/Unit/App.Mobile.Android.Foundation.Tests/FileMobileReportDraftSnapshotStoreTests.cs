namespace App.Mobile.Android.Foundation.Tests;

public sealed class FileMobileReportDraftSnapshotStoreTests
{
    [Fact]
    public async Task LoadAsyncReturnsEmptyListWhenFileIsAbsent()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var store = new global::App.Mobile.Android.Services.Local.FileMobileReportDraftSnapshotStore(storageDirectory);

            var loaded = await store.LoadAsync();

            Assert.Empty(loaded);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task SaveAsyncThenLoadAsyncReturnsDraftWithFields()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var store = new global::App.Mobile.Android.Services.Local.FileMobileReportDraftSnapshotStore(storageDirectory);
            var drafts = CreateDrafts(includeAttachment: false);

            await store.SaveAsync(drafts);
            var loaded = await store.LoadAsync();

            var draft = Assert.Single(loaded);
            Assert.Equal("draft-1", draft.DraftId);
            Assert.Equal("Черновик FPV #1", draft.Title);
            Assert.Equal(2, draft.Fields.Count);
            Assert.Contains(draft.Fields, field => field.FieldKey == "device_type" && field.ValueText == "Локальное значение 1");
            Assert.Contains(draft.Fields, field => field.FieldKey == "serial_number" && field.ValueText == "SN-001");
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task SaveAsyncThenLoadAsyncReturnsDraftWithAttachmentMetadata()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var store = new global::App.Mobile.Android.Services.Local.FileMobileReportDraftSnapshotStore(storageDirectory);
            var drafts = CreateDrafts(includeAttachment: true);

            await store.SaveAsync(drafts);
            var loaded = await store.LoadAsync();

            var draft = Assert.Single(loaded);
            var attachment = Assert.Single(draft.Attachments);
            Assert.Equal(global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video, attachment.Kind);
            Assert.Equal("video-1.mp4", attachment.FileName);
            Assert.Equal("video/mp4", attachment.ContentType);
            Assert.Equal("cache-1", attachment.SelectedMediaCacheKey);
            Assert.True(attachment.HasLocalReadHandle);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    [Fact]
    public async Task SaveAsyncEmptyListThenLoadAsyncReturnsEmptyList()
    {
        var storageDirectory = CreateTempDirectory();

        try
        {
            var store = new global::App.Mobile.Android.Services.Local.FileMobileReportDraftSnapshotStore(storageDirectory);
            await store.SaveAsync(CreateDrafts(includeAttachment: true));

            await store.SaveAsync([]);
            var loaded = await store.LoadAsync();

            Assert.Empty(loaded);
        }
        finally
        {
            DeleteTempDirectory(storageDirectory);
        }
    }

    private static IReadOnlyList<global::App.Mobile.Android.Reports.MobileReportDraft> CreateDrafts(bool includeAttachment)
    {
        var now = new DateTimeOffset(2026, 4, 22, 9, 0, 0, TimeSpan.Zero);
        var attachments = includeAttachment
            ? new[]
            {
                new global::App.Mobile.Android.Reports.MobileReportAttachment(
                    AttachmentId: "attachment-1",
                    DraftId: "draft-1",
                    Kind: global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video,
                    FileName: "video-1.mp4",
                    ContentType: "video/mp4",
                    SourceText: "Галерея",
                    AddedAtUtc: now,
                    SelectedMediaCacheKey: "cache-1",
                    HasLocalReadHandle: true)
            }
            : Array.Empty<global::App.Mobile.Android.Reports.MobileReportAttachment>();

        return
        [
            new global::App.Mobile.Android.Reports.MobileReportDraft(
                DraftId: "draft-1",
                CreatedAtUtc: now,
                UpdatedAtUtc: now.AddMinutes(7),
                Title: "Черновик FPV #1",
                Status: global::App.Mobile.Android.Reports.MobileReportDraftStatus.ReadyForAttachmentReview,
                Fields:
                [
                    new global::App.Mobile.Android.Reports.MobileReportDraftFieldValue(
                        FieldKey: "device_type",
                        Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeviceTypeLabel,
                        ValueText: "Локальное значение 1",
                        IsRequired: true,
                        IsPlaceholder: false)
                    {
                        SectionKey = "basic-data",
                        FieldKind = global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
                        SelectorMode = global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
                        LastUpdatedAtUtc = now.AddMinutes(1)
                    },
                    new global::App.Mobile.Android.Reports.MobileReportDraftFieldValue(
                        FieldKey: "serial_number",
                        Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldSerialNumberLabel,
                        ValueText: "SN-001",
                        IsRequired: true,
                        IsPlaceholder: false)
                    {
                        SectionKey = "basic-data",
                        FieldKind = global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Text,
                        SelectorMode = global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
                        LastUpdatedAtUtc = now.AddMinutes(2)
                    }
                ],
                Attachments: attachments)
            {
                IsRestoredFromSnapshot = false
            }
        ];
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"androida-mobile-report-drafts-{Guid.NewGuid():N}");
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
}