using System.Text.RegularExpressions;

namespace App.Web.Features.Upload.ControlPlane;

public static partial class UploadControlPlaneErrorRedactor
{
    private const int MaxSanitizedLength = 2048;

    public static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = SensitiveJsonPropertyRegex().Replace(value, "$1***REDACTED***$2");
        sanitized = BearerTokenRegex().Replace(sanitized, "Bearer ***REDACTED***");

        if (sanitized.Length <= MaxSanitizedLength)
        {
            return sanitized;
        }

        return sanitized[..MaxSanitizedLength] + "...";
    }

    [GeneratedRegex("""(?i)("(?:accessToken|refreshToken|password|token)"\s*:\s*")[^"]*(")""")]
    private static partial Regex SensitiveJsonPropertyRegex();

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*")]
    private static partial Regex BearerTokenRegex();
}