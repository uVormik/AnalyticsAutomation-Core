using System.Globalization;
using App.Web.Features.Upload.Models;
using Xunit;

namespace App.Web.Tests;

public sealed class UploadPreCheckRequestFactoryTests
{
    [Fact]
    public void Create_builds_request_using_frozen_contract_fields()
    {
        var form = new UploadPreCheckFormModel
        {
            UserId = "11111111-1111-1111-1111-111111111111",
            DeviceId = "22222222-2222-2222-2222-222222222222",
            GroupNodeId = "33333333-3333-3333-3333-333333333333",
            BusinessObjectKey = "demo-business-object",
            FileName = "sample.mp4",
            SizeBytes = 1048576,
            ByteSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ContentType = "video/mp4",
            CapturedAtUtc = "2026-04-20T12:00:00Z"
        };

        var request = UploadPreCheckRequestFactory.Create(form);
        var expectedCapturedAtUtc = DateTimeOffset.Parse(
            form.CapturedAtUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        Assert.Equal(Guid.Parse(form.UserId), request.UserId);
        Assert.True(request.DeviceId.HasValue);
        Assert.Equal(Guid.Parse(form.DeviceId), request.DeviceId.Value);
        Assert.True(request.GroupNodeId.HasValue);
        Assert.Equal(Guid.Parse(form.GroupNodeId), request.GroupNodeId.Value);
        Assert.Equal(form.BusinessObjectKey, request.BusinessObjectKey);
        Assert.Equal(form.FileName, request.FileName);
        Assert.Equal(form.SizeBytes, request.SizeBytes);
        Assert.Equal(form.ByteSha256, request.ByteSha256);
        Assert.Equal(form.ContentType, request.ContentType);
        Assert.Equal(expectedCapturedAtUtc, request.CapturedAtUtc);
    }

    [Fact]
    public void Create_allows_empty_optional_device_and_group_node_ids()
    {
        var form = CreateValidForm();
        form.DeviceId = string.Empty;
        form.GroupNodeId = string.Empty;

        var request = UploadPreCheckRequestFactory.Create(form);

        Assert.Null(request.DeviceId);
        Assert.Null(request.GroupNodeId);
    }

    [Fact]
    public void Create_rejects_invalid_guid_before_calling_backend()
    {
        var form = CreateValidForm();
        form.UserId = "not-a-guid";

        Assert.Throws<InvalidOperationException>(() => UploadPreCheckRequestFactory.Create(form));
    }

    [Fact]
    public void Create_rejects_invalid_captured_at_utc_before_calling_backend()
    {
        var form = CreateValidForm();
        form.CapturedAtUtc = "not-a-date";

        Assert.Throws<InvalidOperationException>(() => UploadPreCheckRequestFactory.Create(form));
    }

    private static UploadPreCheckFormModel CreateValidForm() => new()
    {
        UserId = "11111111-1111-1111-1111-111111111111",
        DeviceId = "22222222-2222-2222-2222-222222222222",
        GroupNodeId = "33333333-3333-3333-3333-333333333333",
        BusinessObjectKey = "demo-business-object",
        FileName = "sample.mp4",
        SizeBytes = 1048576,
        ByteSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        ContentType = "video/mp4",
        CapturedAtUtc = "2026-04-20T12:00:00Z"
    };
}