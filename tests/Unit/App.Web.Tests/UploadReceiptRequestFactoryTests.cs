using System.Globalization;

using App.Web.Features.Upload.Models;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadReceiptRequestFactoryTests
{
    [Fact]
    public void Create_builds_request_using_frozen_contract_fields()
    {
        var form = CreateValidForm();

        var request = UploadReceiptRequestFactory.Create(form);

        Assert.Equal(Guid.Parse(form.PreUploadCheckId), request.PreUploadCheckId);
        Assert.Equal(Guid.Parse(form.UserId), request.UserId);
        Assert.True(request.DeviceId.HasValue);
        Assert.Equal(Guid.Parse(form.DeviceId), request.DeviceId.Value);
        Assert.True(request.GroupNodeId.HasValue);
        Assert.Equal(Guid.Parse(form.GroupNodeId), request.GroupNodeId.Value);
        Assert.Equal(form.ExternalVideoId, request.ExternalVideoId);
        Assert.Equal(form.StorageKey, request.StorageKey);
        Assert.Equal(form.SiteStatus, request.SiteStatus);
        Assert.Equal(form.SizeBytes, request.SizeBytes);
        Assert.Equal(form.ByteSha256, request.ByteSha256);
        Assert.Equal(form.IdempotencyKey, request.IdempotencyKey);
        Assert.Equal(ParseExpectedDate(form.UploadedAtUtc), request.UploadedAtUtc);
    }

    [Fact]
    public void Create_rejects_invalid_pre_upload_check_id_before_calling_backend()
    {
        var form = CreateValidForm();
        form.PreUploadCheckId = "not-a-guid";

        Assert.Throws<InvalidOperationException>(() => UploadReceiptRequestFactory.Create(form));
    }

    [Fact]
    public void Create_rejects_invalid_uploaded_at_utc_before_calling_backend()
    {
        var form = CreateValidForm();
        form.UploadedAtUtc = "not-a-date";

        Assert.Throws<InvalidOperationException>(() => UploadReceiptRequestFactory.Create(form));
    }

    private static DateTimeOffset ParseExpectedDate(string value) =>
        DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private static UploadReceiptFormModel CreateValidForm() => new()
    {
        PreUploadCheckId = "44444444-4444-4444-4444-444444444444",
        ClientReceiptKey = "receipt-client-key-1",
        IdempotencyKey = "receipt-idempotency-key-1",
        UserId = "11111111-1111-1111-1111-111111111111",
        DeviceId = "22222222-2222-2222-2222-222222222222",
        GroupNodeId = "33333333-3333-3333-3333-333333333333",
        BusinessObjectKey = "demo-business-object",
        ExternalVideoId = "site-video-demo",
        FileName = "sample.mp4",
        ContentType = "video/mp4",
        StorageKey = "videos/site-video-demo.mp4",
        SiteStatus = "uploaded",
        SizeBytes = 1048576,
        ByteSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        UploadedAtUtc = "2026-04-20T12:00:00Z"
    };
}