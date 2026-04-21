using App.Web.Features.Upload.SiteGateway;

using Xunit;

namespace App.Web.Tests;

public sealed class DirectSiteVideoUploadAdapterTests
{
    [Fact]
    public async Task Disabled_adapter_does_not_claim_production_site_upload_success()
    {
        var adapter = new DisabledDirectSiteVideoUploadAdapter();
        var draft = new DirectSiteVideoUploadDraft(
            PreUploadCheckId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            FileName: "sample.mp4",
            SizeBytes: 1048576,
            ByteSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ContentType: "video/mp4");

        await using var content = new MemoryStream(Array.Empty<byte>());

        var result = await adapter.UploadAsync(draft, content);

        Assert.False(result.IsConfigured);
        Assert.False(result.Succeeded);
        Assert.Null(result.ExternalVideoId);
        Assert.Null(result.StorageKey);
        Assert.Null(result.SiteStatus);
        Assert.Contains("not configured", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Disabled_adapter_still_validates_that_stream_is_readable()
    {
        var adapter = new DisabledDirectSiteVideoUploadAdapter();
        var draft = new DirectSiteVideoUploadDraft(
            PreUploadCheckId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            FileName: "sample.mp4",
            SizeBytes: 1048576,
            ByteSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ContentType: "video/mp4");

        await using var content = new NonReadableStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.UploadAsync(draft, content));
    }

    private sealed class NonReadableStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}