using App.Web.Features.Upload.Api;
using Xunit;

namespace App.Web.Tests;

public sealed class UploadApiEndpointsTests
{
    [Fact]
    public void Upload_api_endpoints_match_backend_handoff_routes()
    {
        Assert.Equal("/api/video/pre-upload-check", UploadApiEndpoints.PreUploadCheck);
        Assert.Equal("/api/video/upload-receipt", UploadApiEndpoints.UploadReceipt);
    }
}