using App.Web.Features.Upload.ControlPlane;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadControlPlaneErrorRedactorTests
{
    [Fact]
    public void RedactHidesSensitiveJsonFields()
    {
        const string raw = """
        {
            "accessToken": "alpha-sensitive-value",
            "refreshToken": "beta-sensitive-value",
            "password": "gamma-sensitive-value"
        }
        """;

        var sanitized = UploadControlPlaneErrorRedactor.Redact(raw);

        Assert.DoesNotContain("alpha-sensitive-value", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("beta-sensitive-value", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("gamma-sensitive-value", sanitized, StringComparison.Ordinal);
        Assert.Contains("***REDACTED***", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactHidesBearerTokens()
    {
        const string raw = "Authorization: Bearer opaque.sensitive.value";

        var sanitized = UploadControlPlaneErrorRedactor.Redact(raw);

        Assert.DoesNotContain("opaque.sensitive.value", sanitized, StringComparison.Ordinal);
        Assert.Contains("Bearer ***REDACTED***", sanitized, StringComparison.Ordinal);
    }
}