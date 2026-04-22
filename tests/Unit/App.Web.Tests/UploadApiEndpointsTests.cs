using App.Web.Features.Upload.Api;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadApiEndpointsTests
{
    [Fact]
    public void UploadApiEndpointsMatchBackendHandoffRoutes()
    {
        Assert.Equal("/api/video/pre-upload-check", UploadApiEndpoints.PreUploadCheck);
        Assert.Equal("/api/video/upload-receipt", UploadApiEndpoints.UploadReceipt);
    }
}