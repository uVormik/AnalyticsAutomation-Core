using App.Desktop.Boundaries;
using App.Desktop.Composition;
using App.Desktop.Services.Auth;
using App.Desktop.Services.GroupTree;
using App.Desktop.Services.Upload;

using Microsoft.Extensions.DependencyInjection;

namespace App.Desktop.Tests;

public sealed class DesktopDirectSiteUploadReceiptLinkageTests
{
    [Fact]
    public void ValidProviderSuccessCreatesReadyFutureSubmissionLinkage()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = CreateSuccessResponse(request, correlationId);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.True(linkage.IsReadyForFutureServerSubmission);
        Assert.False(linkage.EnablesRealUpload);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageStatus.ReadyForFutureServerSubmission, linkage.Status);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.None, linkage.FailureKind);
        Assert.Equal(request.PreUploadCheckId, linkage.PreUploadCheckId);
        Assert.Equal(request.BusinessObjectKey, linkage.BusinessObjectKey);
        Assert.Equal(response.ProviderUploadId, linkage.ProviderUploadId);
        Assert.Equal(response.ExternalVideoId, linkage.ExternalVideoId);
        Assert.Equal(response.StorageKey, linkage.StorageKey);
        Assert.Equal(request.IdempotencyKey, linkage.IdempotencyKey);
        AssertSafe(linkage.DiagnosticText);
    }

    [Fact]
    public void ReadyLinkageDoesNotClaimServerReceiptAcceptance()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = CreateSuccessResponse(request, correlationId);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);
        string text = linkage.ToString();

        Assert.True(linkage.IsReadyForFutureServerSubmission);
        Assert.Contains("ReadyForFutureServerSubmission", text, StringComparison.Ordinal);
        Assert.DoesNotContain("server receipt accepted", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UploadReceipt accepted", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("App.Api accepted", text, StringComparison.OrdinalIgnoreCase);
        AssertSafe(text);
    }

    [Fact]
    public void MissingPreUploadCheckIdFailsSafely()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = DesktopDirectSiteUploadRequest.FromMetadata(
            CreateTargetWithSignedQuery(correlationId),
            preUploadCheckId: null,
            "business-object-001",
            1024,
            Sha256,
            "video/mp4",
            "idem-001",
            correlationId.ToString("D"),
            "video.mp4");
        DesktopDirectSiteUploadResponse response = CreateSuccessResponse(CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId), correlationId);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.InvalidRequest, linkage.FailureKind);
        AssertSafe(linkage.DiagnosticText);
    }

    [Fact]
    public void MissingExternalVideoIdOrStorageKeyFailsSafely()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromSuccess(
            "korobochka-direct",
            "provider-upload-001",
            externalVideoId: null,
            storageKey: null,
            DesktopDirectSiteUploadResponseStatus.Completed,
            DateTimeOffset.UtcNow,
            request.SizeBytes,
            request.ByteSha256,
            correlationId.ToString("D"));

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.InvalidResponse, linkage.FailureKind);
        AssertSafe(linkage.DiagnosticText);
    }

    [Fact]
    public void SizeMismatchFailsSafely()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = CreateSuccessResponse(request, correlationId, sizeBytes: request.SizeBytes + 1);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.SizeMismatch, linkage.FailureKind);
        AssertSafe(linkage.ToString());
    }

    [Fact]
    public void HashMismatchFailsSafely()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = CreateSuccessResponse(request, correlationId, byteSha256: OtherSha256);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.Sha256Mismatch, linkage.FailureKind);
        AssertSafe(linkage.DiagnosticText);
    }

    [Fact]
    public async Task ProviderUnavailableResponseDoesNotCreateReadyLinkage()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        var client = new DisabledDesktopDirectSiteProviderClient();
        DesktopDirectSiteUploadResponse response = await client.UploadAsync(request, CancellationToken.None);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageStatus.Unavailable, linkage.Status);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderUnavailable, linkage.FailureKind);
        AssertSafe(linkage.ToString());
    }

    [Fact]
    public void ProviderFailureResponseDoesNotCreateReadyLinkage()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromFailure(
            "korobochka-direct",
            DesktopDirectSiteUploadResponseStatus.FailedTerminal,
            retryable: false,
            DesktopDirectSiteUploadFailureKind.Unknown,
            "raw response body marker",
            correlationId.ToString("D"));

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderFailure, linkage.FailureKind);
        AssertSafe(linkage.DiagnosticText);
    }

    [Fact]
    public void RetryableProviderFailureRemainsNotReady()
    {
        Guid correlationId = Guid.NewGuid();
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId);
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromFailure(
            "korobochka-direct",
            DesktopDirectSiteUploadResponseStatus.FailedRetryable,
            retryable: true,
            DesktopDirectSiteUploadFailureKind.ProviderTemporary,
            "temporary provider failure",
            correlationId.ToString("D"));

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);

        Assert.False(linkage.IsReadyForFutureServerSubmission);
        Assert.Equal(DesktopDirectSiteUploadReceiptLinkageFailureKind.ProviderRetryableFailure, linkage.FailureKind);
        AssertSafe(linkage.ToString());
    }

    [Fact]
    public void DiagnosticsAndToStringAreRedacted()
    {
        Guid correlationId = Guid.NewGuid();
        string localPath = string.Concat("D:", "\\", "capture", "\\", "clip.mp4");
        DesktopDirectSiteUploadRequest request = CreateRequest(CreateTargetWithSignedQuery(correlationId), correlationId, localPath);
        DesktopDirectSiteUploadResponse response = CreateSuccessResponse(request, correlationId);

        DesktopDirectSiteUploadReceiptLinkage linkage =
            DesktopDirectSiteUploadReceiptLinkage.FromRequestAndResponse(request, response);
        string text = linkage.DiagnosticText;

        Assert.True(linkage.IsReadyForFutureServerSubmission);
        Assert.DoesNotContain("query-secret-marker", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(localPath, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", text, StringComparison.OrdinalIgnoreCase);
        AssertSafe(text);
    }

    [Fact]
    public void ExistingDirectSiteClientsRemainDisabledInComposition()
    {
        using ServiceProvider services = DesktopCompositionRoot.BuildServices(
            DesktopAuthOptions.FromControlPlaneBaseAddress("https://control-plane.local"),
            DesktopUploadSectionOptions.Disabled,
            DesktopGroupTreeOptions.EnabledForLiveControlPlane,
            DesktopDirectSiteProviderOptions.FromEnvironmentValues(
                "korobochka-direct",
                "https://upload.korobochka.local"));

        Assert.IsType<DisabledDesktopDirectSiteUploadClient>(
            services.GetRequiredService<IDesktopDirectSiteUploadClient>());
        Assert.IsType<DisabledDesktopDirectSiteProviderClient>(
            services.GetRequiredService<IDesktopDirectSiteProviderClient>());
    }

    private const string Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string OtherSha256 = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    private static DesktopDirectSiteUploadTarget CreateTargetWithSignedQuery(Guid correlationId)
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
            correlationId.ToString("D"));
    }

    private static DesktopDirectSiteUploadRequest CreateRequest(
        DesktopDirectSiteUploadTarget target,
        Guid correlationId,
        string displayFileName = "video.mp4")
    {
        return DesktopDirectSiteUploadRequest.FromMetadata(
            target,
            Guid.NewGuid().ToString("D"),
            "business-object-001",
            1024,
            Sha256,
            "video/mp4",
            "idem-001",
            correlationId.ToString("D"),
            displayFileName);
    }

    private static DesktopDirectSiteUploadResponse CreateSuccessResponse(
        DesktopDirectSiteUploadRequest request,
        Guid correlationId,
        long? sizeBytes = null,
        string? byteSha256 = null)
    {
        return DesktopDirectSiteUploadResponse.FromSuccess(
            "korobochka-direct",
            "provider-upload-001",
            "external-video-001",
            "site/video-001",
            DesktopDirectSiteUploadResponseStatus.Completed,
            new DateTimeOffset(2026, 5, 11, 10, 0, 0, TimeSpan.Zero),
            sizeBytes ?? request.SizeBytes,
            byteSha256 ?? request.ByteSha256,
            correlationId.ToString("D"));
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