using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Tests;

public sealed class DisabledDesktopDirectSiteProviderClientTests
{
    [Fact]
    public async Task UploadAsyncReturnsUnavailableFailureForValidRequest()
    {
        var client = new DisabledDesktopDirectSiteProviderClient();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(), "video.mp4");

        DesktopDirectSiteUploadResponse response = await client.UploadAsync(request, CancellationToken.None);

        Assert.True(response.IsValid);
        Assert.False(response.IsSuccess);
        Assert.False(response.EnablesRealUpload);
        Assert.Equal(DesktopDirectSiteUploadResponseStatus.FailedTerminal, response.Status);
        Assert.Equal(DesktopDirectSiteUploadFailureKind.ProviderUnavailable, response.FailureKind);
        Assert.False(response.Retryable);
        Assert.Equal("<redacted>", response.FailureMessageRedacted);
        AssertSafe(response.DiagnosticText);
    }

    [Fact]
    public async Task UploadAsyncKeepsInvalidRequestInvalidWithoutUpload()
    {
        var client = new DisabledDesktopDirectSiteProviderClient();

        DesktopDirectSiteUploadResponse response = await client.UploadAsync(
            DesktopDirectSiteUploadRequest.Invalid,
            CancellationToken.None);

        Assert.False(response.IsValid);
        Assert.False(response.EnablesRealUpload);
        Assert.Equal(DesktopDirectSiteUploadResponseStatus.Unknown, response.Status);
        Assert.Equal(DesktopDirectSiteUploadFailureKind.Unknown, response.FailureKind);
        AssertSafe(response.DiagnosticText);
    }

    [Fact]
    public async Task UploadAsyncDoesNotExposeSignedQueryOrLocalPath()
    {
        var client = new DisabledDesktopDirectSiteProviderClient();
        string localPath = string.Concat("D:", "\\", "capture", "\\", "clip.mp4");
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(), localPath);

        DesktopDirectSiteUploadResponse response = await client.UploadAsync(request, CancellationToken.None);
        string diagnosticText = response.ToString();

        Assert.True(response.IsValid);
        Assert.DoesNotContain("query-secret-marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(localPath, diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", diagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(diagnosticText);
    }

    [Fact]
    public async Task UploadAsyncDoesNotExposeSensitiveOrRawBodyMarkers()
    {
        var client = new DisabledDesktopDirectSiteProviderClient();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(), "clip.mp4");

        DesktopDirectSiteUploadResponse response = await client.UploadAsync(request, CancellationToken.None);
        string diagnosticText = response.DiagnosticText;

        Assert.True(response.IsValid);
        Assert.DoesNotContain("sample-password-marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-access-token-marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-refresh-token-marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-authorization-marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-provider-credential-marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw request body marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response body marker", diagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(diagnosticText);
    }

    [Fact]
    public void DiagnosticsAndToStringRemainRedacted()
    {
        var client = new DisabledDesktopDirectSiteProviderClient();

        string diagnosticText = client.DiagnosticText;

        Assert.False(client.EnablesRealUpload);
        Assert.Contains("Status = Disabled", diagnosticText, StringComparison.Ordinal);
        Assert.Contains("Diagnostics = <redacted>", diagnosticText, StringComparison.Ordinal);
        AssertSafe(diagnosticText);
    }

    [Fact]
    public void CompositionResolvesDisabledProviderClientAndKeepsSiteUploadDisabled()
    {
        using ServiceProvider services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress("https://control-plane.local"),
            DesktopUploadSectionOptions.Disabled,
            DesktopGroupTreeOptions.EnabledForLiveControlPlane,
            DesktopDirectSiteProviderOptions.FromEnvironmentValues(
                "korobochka-direct",
                "https://upload.korobochka.local"));

        DesktopDirectSiteProviderOptions options = services.GetRequiredService<DesktopDirectSiteProviderOptions>();

        Assert.True(options.IsProviderConfigPresent);
        Assert.True(options.IsProviderConfigValid);
        Assert.False(options.IsDirectSiteUploadAvailable);
        Assert.IsType<DisabledDesktopDirectSiteProviderClient>(
            services.GetRequiredService<IDesktopDirectSiteProviderClient>());
        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
    }

    private const string Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static DesktopDirectSiteUploadTarget CreateTargetWithSignedQuery()
    {
        string endpointUri = new UriBuilder("https", "upload.korobochka.local")
        {
            Path = "upload/video",
            Query = "signature=query-secret-marker"
        }.Uri.ToString();

        return DesktopDirectSiteUploadTarget.FromMetadata(
            "korobochka-direct",
            endpointUri,
            "POST",
            Guid.NewGuid().ToString("D"));
    }

    private static DesktopDirectSiteUploadRequest CreateRequest(
        DesktopDirectSiteUploadTarget target,
        string displayFileName)
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
            displayFileName);
    }

    private static void AssertSafe(string text)
    {
        Assert.DoesNotContain("query-secret-marker", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw request", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-password-marker", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-access-token-marker", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-refresh-token-marker", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-authorization-marker", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-provider-credential-marker", text, StringComparison.OrdinalIgnoreCase);
    }
}