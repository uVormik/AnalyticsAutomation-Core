using App.Web.Features.Upload.SiteGateway;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace App.Web.Tests;

public sealed class LocalStubDirectSiteVideoUploadAdapterTests
{
    [Fact]
    public async Task UploadAsyncReturnsSucceededLocalStubResultWithoutAppApiProxy()
    {
        var adapter = new LocalStubDirectSiteVideoUploadAdapter(
            NullLogger<LocalStubDirectSiteVideoUploadAdapter>.Instance);

        await using var content = new MemoryStream(Array.Empty<byte>());

        var result = await adapter.UploadAsync(CreateDraft(), content);

        Assert.True(result.IsConfigured);
        Assert.True(result.Succeeded);
        Assert.Contains("No video bytes were sent through App.Api", result.Message, StringComparison.Ordinal);
        Assert.StartsWith("local-stub-", result.ExternalVideoId, StringComparison.Ordinal);
        Assert.StartsWith("local-stub/", result.StorageKey, StringComparison.Ordinal);
        Assert.Equal(LocalStubDirectSiteVideoUploadAdapter.LocalStubSiteStatus, result.SiteStatus);
    }

    [Fact]
    public async Task UploadAsyncReturnsFailureWhenContentStreamIsNotReadable()
    {
        var adapter = new LocalStubDirectSiteVideoUploadAdapter(
            NullLogger<LocalStubDirectSiteVideoUploadAdapter>.Instance);

        await using var content = new NonReadableStream();

        var result = await adapter.UploadAsync(CreateDraft(), content);

        Assert.True(result.IsConfigured);
        Assert.False(result.Succeeded);
        Assert.Contains("readable content stream", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAsyncSanitizesStorageKeyFileNameSegment()
    {
        var adapter = new LocalStubDirectSiteVideoUploadAdapter(
            NullLogger<LocalStubDirectSiteVideoUploadAdapter>.Instance);

        await using var content = new MemoryStream(Array.Empty<byte>());

        var result = await adapter.UploadAsync(
            CreateDraft(fileName: "sample:video?.mp4"),
            content);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain("?", result.StorageKey, StringComparison.Ordinal);
        Assert.DoesNotContain(":", result.StorageKey, StringComparison.Ordinal);
    }

    private static DirectSiteVideoUploadDraft CreateDraft(
        string fileName = "sample.mp4") =>
        new(
            PreUploadCheckId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            FileName: fileName,
            SizeBytes: 1048576,
            ByteSha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ContentType: "video/mp4");

    private sealed class NonReadableStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count) =>
            throw new NotSupportedException();

        public override long Seek(
            long offset,
            SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(
            byte[] buffer,
            int offset,
            int count) =>
            throw new NotSupportedException();
    }
}