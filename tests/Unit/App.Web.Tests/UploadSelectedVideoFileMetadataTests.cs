using System.Globalization;
using System.Text;

using App.Web.Features.Upload.Models;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadSelectedVideoFileMetadataTests
{
    [Fact]
    public void CreateInfersKnownVideoContentTypeAndNormalizesSha256()
    {
        var metadata = UploadSelectedVideoFileMetadataFactory.Create(
            "clip.mp4",
            1024,
            contentType: null,
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            DateTimeOffset.Parse("2026-04-24T10:00:00+03:00", CultureInfo.InvariantCulture));

        Assert.Equal("clip.mp4", metadata.FileName);
        Assert.Equal(1024, metadata.SizeBytes);
        Assert.Equal("video/mp4", metadata.ContentType);
        Assert.Equal(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            metadata.ByteSha256);
        Assert.Equal(TimeSpan.Zero, metadata.CapturedAtUtc.Offset);
    }

    [Fact]
    public void CreateRejectsNonVideoFile()
    {
        Assert.Throws<InvalidOperationException>(() =>
            UploadSelectedVideoFileMetadataFactory.Create(
                "notes.txt",
                1024,
                "text/plain",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task ComputeSha256HexAsyncReturnsLowercaseHex()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        string hash = await UploadSelectedVideoFileMetadataFactory.ComputeSha256HexAsync(stream);

        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            hash);
    }

    [Fact]
    public void ApplySelectedFileAutofillsPreCheckForm()
    {
        var form = new UploadPreCheckFormModel();
        var metadata = UploadSelectedVideoFileMetadataFactory.Create(
            "clip.webm",
            2048,
            "video/webm",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            DateTimeOffset.Parse("2026-04-24T10:00:00Z", CultureInfo.InvariantCulture));

        UploadPreCheckFormAutoFill.ApplySelectedFile(form, metadata);

        Assert.Equal(metadata.FileName, form.FileName);
        Assert.Equal(metadata.SizeBytes, form.SizeBytes);
        Assert.Equal(metadata.ContentType, form.ContentType);
        Assert.Equal(metadata.ByteSha256, form.ByteSha256);
        Assert.Equal("2026-04-24T10:00:00.0000000+00:00", form.CapturedAtUtc);
    }
}