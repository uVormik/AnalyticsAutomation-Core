using App.Desktop.Services.Upload;

namespace App.Desktop.Tests;

public sealed class DesktopDirectSiteUploadResponseTests
{
    [Fact]
    public void SuccessCreatesSafeRedactedDiagnostics()
    {
        DateTimeOffset uploadedAtUtc = new(2026, 5, 11, 10, 0, 0, TimeSpan.Zero);

        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromSuccess(
            "korobochka-direct",
            "provider-upload-001",
            "external-video-001",
            "site/video-001",
            DesktopDirectSiteUploadResponseStatus.Completed,
            uploadedAtUtc,
            2048,
            Sha256,
            Guid.NewGuid().ToString("D"));

        Assert.True(response.IsValid);
        Assert.True(response.IsSuccess);
        Assert.False(response.EnablesRealUpload);
        Assert.False(response.Retryable);
        Assert.Equal(DesktopDirectSiteUploadFailureKind.None, response.FailureKind);
        AssertSafe(response.DiagnosticText);
    }

    [Fact]
    public void FailureClassificationIsLocalAndSafe()
    {
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromFailure(
            "korobochka-direct",
            DesktopDirectSiteUploadResponseStatus.FailedRetryable,
            retryable: true,
            DesktopDirectSiteUploadFailureKind.ProviderTemporary,
            "temporary provider failure with raw response body",
            Guid.NewGuid().ToString("D"));

        Assert.True(response.IsValid);
        Assert.False(response.IsSuccess);
        Assert.False(response.EnablesRealUpload);
        Assert.True(response.Retryable);
        Assert.Equal(DesktopDirectSiteUploadFailureKind.ProviderTemporary, response.FailureKind);
        Assert.Equal("<redacted>", response.FailureMessageRedacted);
        Assert.DoesNotContain("temporary provider failure", response.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response body", response.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(response.ToString());
    }

    [Fact]
    public void UnsafeProviderKeyOrMetadataKeepsResponseInvalid()
    {
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromSuccess(
            "token-provider",
            "provider-upload-001",
            "external-video-001",
            "site/video-001",
            DesktopDirectSiteUploadResponseStatus.Completed,
            DateTimeOffset.UtcNow,
            2048,
            Sha256,
            Guid.NewGuid().ToString("D"));

        Assert.False(response.IsValid);
        Assert.False(response.EnablesRealUpload);
        Assert.Null(response.ProviderKey);
        AssertSafe(response.DiagnosticText);
    }

    [Fact]
    public void SignedUriLikeValuesAreNotExposedByResponse()
    {
        string providerUploadId = new UriBuilder("https", "upload.korobochka.local")
        {
            Path = "receipt",
            Query = "query=redacted-test-value"
        }.Uri.ToString();

        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromSuccess(
            "korobochka-direct",
            providerUploadId,
            "external-video-001",
            "site/video-001",
            DesktopDirectSiteUploadResponseStatus.Completed,
            DateTimeOffset.UtcNow,
            2048,
            Sha256,
            Guid.NewGuid().ToString("D"));

        Assert.False(response.IsValid);
        Assert.False(response.EnablesRealUpload);
        Assert.DoesNotContain("redacted-test-value", response.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(response.ToString());
    }

    [Fact]
    public void RawBodyInputIsNotStoredOrDisplayed()
    {
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromFailure(
            "korobochka-direct",
            DesktopDirectSiteUploadResponseStatus.FailedTerminal,
            retryable: false,
            DesktopDirectSiteUploadFailureKind.Unknown,
            "raw response body should not be visible",
            Guid.NewGuid().ToString("D"));

        Assert.True(response.IsValid);
        Assert.False(response.EnablesRealUpload);
        Assert.Equal("<redacted>", response.FailureMessageRedacted);
        Assert.DoesNotContain("raw response body", response.DiagnosticText, StringComparison.OrdinalIgnoreCase);
        AssertSafe(response.DiagnosticText);
    }

    [Fact]
    public void ResponseToStringIsSafe()
    {
        DesktopDirectSiteUploadResponse response = DesktopDirectSiteUploadResponse.FromFailure(
            "korobochka-direct",
            DesktopDirectSiteUploadResponseStatus.Rejected,
            retryable: false,
            DesktopDirectSiteUploadFailureKind.Validation,
            "validation detail that must stay redacted",
            Guid.NewGuid().ToString("D"));

        string text = response.ToString();

        Assert.True(response.IsValid);
        Assert.Contains("FailureMessage = <redacted>", text, StringComparison.Ordinal);
        Assert.DoesNotContain("validation detail", text, StringComparison.OrdinalIgnoreCase);
        AssertSafe(text);
    }

    private const string Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static void AssertSafe(string text)
    {
        Assert.DoesNotContain("redacted-test-value", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\capture", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw request", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response body", text, StringComparison.OrdinalIgnoreCase);
    }
}