using App.Web.Features.Upload.ControlPlane;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadControlPlaneEndpointsTests
{
    [Fact]
    public void EndpointsMatchBackendControlPlaneRoutes()
    {
        Assert.Equal("/api/auth/sign-in", UploadControlPlaneEndpoints.SignIn);
        Assert.Equal("/api/auth/refresh", UploadControlPlaneEndpoints.Refresh);
        Assert.Equal("/api/group-tree/nodes", UploadControlPlaneEndpoints.GroupTreeNodes);
        Assert.Equal("/api/group-tree/routing-preview", UploadControlPlaneEndpoints.GroupTreeRoutingPreview);
    }
}