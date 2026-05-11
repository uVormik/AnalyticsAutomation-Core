using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Tests;

public sealed class DesktopDirectSiteUploadRequestTests
{
    [Fact]
    public void MissingRequiredFieldsKeepRequestInvalid()
    {
        DesktopDirectSiteUploadRequest request = DesktopDirectSiteUploadRequest.FromMetadata(
            CreateTarget(),
            preUploadCheckId: null,
            businessObjectKey: "",
            sizeBytes: 0,
            byteSha256: "",
            contentType: "",
            idempotencyKey: "",
            correlationId: "",
            displayFileName: "");

        Assert.False(request.IsValid);
        Assert.False(request.EnablesRealUpload);
        Assert.Null(request.ProviderKey);
        AssertSafe(request.DiagnosticText);
    }

    [Fact]
    public void UnsafeProviderTargetKeepsRequestInvalid()
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "token-provider",
            "https://upload.korobochka.local/upload",
            "POST",
            correlationId: null);

        DesktopDirectSiteUploadRequest request = CreateRequest(target);

        Assert.False(request.IsValid);
        Assert.False(request.EnablesRealUpload);
        Assert.Same(DesktopDirectSiteUploadTarget.Unavailable, request.UploadTarget);
        AssertSafe(request.ToString());
    }

    [Fact]
    public void LocalPathInputIsReducedToDisplayFileNameAndDiagnosticsAreSafe()
    {
        string localPath = string.Concat("D:", "\\", "capture", "\\", "clip.mp4");

        DesktopDirectSiteUploadRequest request = DesktopDirectSiteUploadRequest.FromMetadata(
            CreateTarget(),
            Guid.NewGuid().ToString("D"),
            "business-object-001",
            1024,
            Sha256,
            "video/mp4",
            "idem-001",
            Guid.NewGuid().ToString("D"),
            localPath);

        Assert.True(request.IsValid);
        Assert.Equal("clip.mp4", request.DisplayFileName);
        Assert.DoesNotContain(localPath, request.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", request.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(request.DiagnosticText);
    }

    [Fact]
    public void SignedTargetQueryIsRedactedFromRequestDiagnostics()
    {
        string endpointUri = new UriBuilder("https", "upload.korobochka.local")
        {
            Path = "upload/video",
            Query = "query=redacted-test-value"
        }.Uri.ToString();

        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            endpointUri,
            "PUT",
            Guid.NewGuid().ToString("D"));

        DesktopDirectSiteUploadRequest request = CreateRequest(target);

        Assert.True(request.IsValid);
        Assert.Contains("?<redacted>", request.RedactedUploadTarget, StringComparison.Ordinal);
        Assert.DoesNotContain("redacted-test-value", request.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(request.ToString());
    }

    [Fact]
    public void ValidRequestIsLocalValueObjectOnly()
    {
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTarget());

        Assert.True(request.IsValid);
        Assert.False(request.EnablesRealUpload);
        Assert.Equal("korobochka-direct", request.ProviderKey);
        Assert.Equal("video.mp4", request.DisplayFileName);
        Assert.Equal("video/mp4", request.ContentType);
        Assert.Equal(Sha256, request.ByteSha256);
        AssertSafe(request.DiagnosticText);
    }

    [Fact]
    public void ValidRequestDoesNotReplaceDisabledDirectSiteUploadClient()
    {
        _ = CreateRequest(CreateTarget());

        using ServiceProvider services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress("https://control-plane.local"),
            DesktopUploadSectionOptions.Disabled,
            DesktopGroupTreeOptions.EnabledForLiveControlPlane,
            DesktopDirectSiteProviderOptions.FromEnvironmentValues(
                "korobochka-direct",
                "https://upload.korobochka.local"));

        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
    }

    private const string Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static DesktopDirectSiteUploadTarget CreateTarget()
    {
        return DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            "https://upload.korobochka.local/upload/video",
            "POST",
            Guid.NewGuid().ToString("D"));
    }

    private static DesktopDirectSiteUploadRequest CreateRequest(DesktopDirectSiteUploadTarget target)
    {
        return DesktopDirectSiteUploadRequest.FromMetadata(
            target,
            Guid.NewGuid().ToString("D"),
            "business-object-001",
            1024,
            Sha256,
            "video/mp4",
            "idem-001",
            Guid.NewGuid().ToString("D"),
            "video.mp4");
    }

    private static void AssertSafe(string text)
    {
        Assert.DoesNotContain("redacted-test-value", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw request", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response", text, StringComparison.OrdinalIgnoreCase);
    }
}