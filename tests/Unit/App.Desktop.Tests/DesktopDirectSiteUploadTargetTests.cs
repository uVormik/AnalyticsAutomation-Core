using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Tests;

public sealed class DesktopDirectSiteUploadTargetTests
{
    [Fact]
    public void EmptyMetadataKeepsUploadTargetUnavailable()
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            providerKey: null,
            endpointUri: null,
            httpMethod: null,
            correlationId: null);

        Assert.Same(DesktopDirectSiteUploadTarget.Unavailable, target);
        Assert.False(target.IsMetadataPresent);
        Assert.False(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.Equal("<unavailable>", target.RedactedDisplayUri);
        AssertRedacted(target.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("provider key")]
    [InlineData("provider/key")]
    [InlineData("token-provider")]
    public void UnsafeProviderKeyKeepsUploadTargetUnavailable(string? providerKey)
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            providerKey,
            "https://upload.korobochka.local/upload",
            "POST",
            correlationId: null);

        Assert.True(target.IsMetadataPresent);
        Assert.False(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.Equal("<unavailable>", target.RedactedDisplayUri);
        Assert.Null(target.ProviderKey);
        AssertRedacted(target.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a uri")]
    [InlineData("http://upload.korobochka.local/upload")]
    [InlineData("ftp://upload.korobochka.local/upload")]
    [InlineData("https://user-info@upload.korobochka.local/upload")]
    [InlineData("https://upload.korobochka.local/upload#fragment")]
    public void UnsafeEndpointKeepsUploadTargetUnavailable(string endpointUri)
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            endpointUri,
            "POST",
            correlationId: null);

        Assert.True(target.IsMetadataPresent);
        Assert.False(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.Equal("<unavailable>", target.RedactedDisplayUri);
        AssertRedacted(target.ToString());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void UnsupportedMethodKeepsUploadTargetUnavailable(string httpMethod)
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            "https://upload.korobochka.local/upload",
            httpMethod,
            correlationId: null);

        Assert.True(target.IsMetadataPresent);
        Assert.False(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.Equal("<unavailable>", target.RedactedDisplayUri);
        AssertRedacted(target.ToString());
    }

    [Fact]
    public void InvalidCorrelationIdKeepsUploadTargetUnavailable()
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            "https://upload.korobochka.local/upload",
            "POST",
            "not-a-guid");

        Assert.True(target.IsMetadataPresent);
        Assert.False(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.False(target.HasCorrelationId);
        Assert.Equal("<unavailable>", target.RedactedDisplayUri);
        AssertRedacted(target.DiagnosticText);
    }

    [Fact]
    public void SignedQueryIsRedactedFromDisplayAndDiagnostics()
    {
        string endpointUri = new UriBuilder("https", "upload.korobochka.local")
        {
            Path = "upload/video",
            Query = "signature=abc&expires=123"
        }.Uri.ToString();

        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            endpointUri,
            "PUT",
            Guid.NewGuid().ToString("D"));

        Assert.True(target.IsMetadataPresent);
        Assert.True(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.True(target.HasEndpointQuery);
        Assert.True(target.HasCorrelationId);
        Assert.Equal("korobochka-direct", target.ProviderKey);
        Assert.Equal("https", target.EndpointScheme);
        Assert.Equal("upload.korobochka.local", target.EndpointHost);
        Assert.Equal("PUT", target.HttpMethod);
        Assert.Equal("https://upload.korobochka.local/upload/video?<redacted>", target.RedactedDisplayUri);
        Assert.DoesNotContain("signature=abc", target.RedactedDisplayUri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=123", target.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertRedacted(target.DiagnosticText);
    }

    [Fact]
    public void LocalPathLikeEndpointIsNotExposedInDiagnostics()
    {
        string localPath = string.Concat("D:", "\\", "capture", "\\", "video.mp4");

        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            localPath,
            "POST",
            correlationId: null);

        Assert.True(target.IsMetadataPresent);
        Assert.False(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.Equal("<unavailable>", target.RedactedDisplayUri);
        Assert.DoesNotContain(localPath, target.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("video.mp4", target.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertRedacted(target.DiagnosticText);
    }

    [Fact]
    public void ValidTargetCreatesSafeRedactedDisplayOnly()
    {
        DesktopDirectSiteUploadTarget target = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            "https://upload.korobochka.local/upload/video",
            httpMethod: null,
            correlationId: null);

        Assert.True(target.IsMetadataPresent);
        Assert.True(target.IsValid);
        Assert.False(target.EnablesRealUpload);
        Assert.Equal("korobochka-direct", target.ProviderKey);
        Assert.Equal("https", target.EndpointScheme);
        Assert.Equal("upload.korobochka.local", target.EndpointHost);
        Assert.Equal("/upload/video", target.EndpointPath);
        Assert.False(target.HasEndpointQuery);
        Assert.Equal("POST", target.HttpMethod);
        Assert.Equal("https://upload.korobochka.local/upload/video", target.RedactedDisplayUri);
        AssertRedacted(target.ToString());
    }

    [Fact]
    public void ValidTargetDoesNotReplaceDisabledDirectSiteUploadClient()
    {
        _ = DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            "https://upload.korobochka.local/upload/video",
            "POST",
            Guid.NewGuid().ToString("D"));

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

    private static void AssertRedacted(string text)
    {
        Assert.DoesNotContain("signature=abc", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=123", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw request", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response", text, StringComparison.OrdinalIgnoreCase);
    }
}