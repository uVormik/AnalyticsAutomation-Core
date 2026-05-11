using System.Globalization;

namespace App.Desktop.Services.Upload;

public sealed class DesktopDirectSiteUploadTarget
{
    private const string DefaultHttpMethod = "POST";
    private const string RedactedQueryMarker = "<redacted>";
    private const string UnavailableDisplay = "<unavailable>";

    private static readonly string[] TokenLikeProviderKeySegments =
    [
        "authorization",
        "credential",
        "password",
        "refresh",
        "secret",
        "token"
    ];

    private static readonly string[] AllowedHttpMethods =
    [
        "POST",
        "PUT"
    ];

    private DesktopDirectSiteUploadTarget(
        bool isMetadataPresent,
        bool isValid,
        string? providerKey,
        string? endpointScheme,
        string? endpointHost,
        string? endpointPath,
        bool hasEndpointQuery,
        string? httpMethod,
        bool hasCorrelationId,
        string redactedDisplayUri)
    {
        IsMetadataPresent = isMetadataPresent;
        IsValid = isValid;
        ProviderKey = providerKey;
        EndpointScheme = endpointScheme;
        EndpointHost = endpointHost;
        EndpointPath = endpointPath;
        HasEndpointQuery = hasEndpointQuery;
        HttpMethod = httpMethod;
        HasCorrelationId = hasCorrelationId;
        RedactedDisplayUri = redactedDisplayUri;
    }

    public static DesktopDirectSiteUploadTarget Unavailable { get; } = new(
        isMetadataPresent: false,
        isValid: false,
        providerKey: null,
        endpointScheme: null,
        endpointHost: null,
        endpointPath: null,
        hasEndpointQuery: false,
        httpMethod: null,
        hasCorrelationId: false,
        redactedDisplayUri: UnavailableDisplay);

    public bool IsMetadataPresent { get; }

    public bool IsValid { get; }

    public bool EnablesRealUpload { get; }

    public string? ProviderKey { get; }

    public string? EndpointScheme { get; }

    public string? EndpointHost { get; }

    public string? EndpointPath { get; }

    public bool HasEndpointQuery { get; }

    public string? HttpMethod { get; }

    public bool HasCorrelationId { get; }

    public string RedactedDisplayUri { get; }

    public string DiagnosticText => ToString();

    public static DesktopDirectSiteUploadTarget FromMetadata(
        string? providerKey,
        string? endpointUri,
        string? httpMethod,
        string? correlationId)
    {
        bool isMetadataPresent = !string.IsNullOrWhiteSpace(providerKey)
            || !string.IsNullOrWhiteSpace(endpointUri)
            || !string.IsNullOrWhiteSpace(httpMethod)
            || !string.IsNullOrWhiteSpace(correlationId);

        if (!isMetadataPresent)
        {
            return Unavailable;
        }

        string? normalizedProviderKey = NormalizeProviderKey(providerKey);
        Uri? parsedEndpointUri = ParseEndpointUri(endpointUri);
        string? normalizedHttpMethod = NormalizeHttpMethod(httpMethod);
        bool hasCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? false
            : Guid.TryParse(correlationId.Trim(), out _);

        if (normalizedProviderKey is null
            || parsedEndpointUri is null
            || normalizedHttpMethod is null
            || (!string.IsNullOrWhiteSpace(correlationId) && !hasCorrelationId))
        {
            return InvalidMetadataPresent;
        }

        return new DesktopDirectSiteUploadTarget(
            isMetadataPresent: true,
            isValid: true,
            normalizedProviderKey,
            parsedEndpointUri.Scheme,
            parsedEndpointUri.Host,
            NormalizeEndpointPath(parsedEndpointUri),
            !string.IsNullOrEmpty(parsedEndpointUri.Query),
            normalizedHttpMethod,
            hasCorrelationId,
            BuildRedactedDisplayUri(parsedEndpointUri));
    }

    public override string ToString()
    {
        return $"{nameof(DesktopDirectSiteUploadTarget)} {{ "
            + $"IsMetadataPresent = {IsMetadataPresent}, "
            + $"IsValid = {IsValid}, "
            + $"EnablesRealUpload = {EnablesRealUpload}, "
            + $"ProviderKey = {ProviderKey ?? "<none>"}, "
            + $"Endpoint = {RedactedDisplayUri}, "
            + $"Method = {HttpMethod ?? "<none>"}, "
            + $"HasEndpointQuery = {HasEndpointQuery}, "
            + $"HasCorrelationId = {HasCorrelationId} }}";
    }

    private static DesktopDirectSiteUploadTarget InvalidMetadataPresent { get; } = new(
        isMetadataPresent: true,
        isValid: false,
        providerKey: null,
        endpointScheme: null,
        endpointHost: null,
        endpointPath: null,
        hasEndpointQuery: false,
        httpMethod: null,
        hasCorrelationId: false,
        redactedDisplayUri: UnavailableDisplay);

    private static string? NormalizeProviderKey(string? providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            return null;
        }

        string trimmed = providerKey.Trim();

        if (trimmed.Length is < 3 or > 64 || !char.IsLetterOrDigit(trimmed[0]))
        {
            return null;
        }

        foreach (char character in trimmed)
        {
            if (!char.IsLetterOrDigit(character)
                && character is not '-' and not '_' and not '.')
            {
                return null;
            }
        }

        foreach (string tokenLikeSegment in TokenLikeProviderKeySegments)
        {
            if (trimmed.Contains(tokenLikeSegment, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return trimmed;
    }

    private static Uri? ParseEndpointUri(string? endpointUri)
    {
        if (string.IsNullOrWhiteSpace(endpointUri)
            || !Uri.TryCreate(endpointUri.Trim(), UriKind.Absolute, out Uri? parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(parsed.Host)
            || !string.IsNullOrWhiteSpace(parsed.UserInfo)
            || !string.IsNullOrWhiteSpace(parsed.Fragment))
        {
            return null;
        }

        return parsed;
    }

    private static string? NormalizeHttpMethod(string? httpMethod)
    {
        if (string.IsNullOrWhiteSpace(httpMethod))
        {
            return DefaultHttpMethod;
        }

        string normalized = httpMethod.Trim().ToUpperInvariant();

        foreach (string allowedMethod in AllowedHttpMethods)
        {
            if (string.Equals(normalized, allowedMethod, StringComparison.Ordinal))
            {
                return normalized;
            }
        }

        return null;
    }

    private static string NormalizeEndpointPath(Uri endpointUri)
    {
        return endpointUri.AbsolutePath is "" or "/"
            ? "/"
            : endpointUri.AbsolutePath;
    }

    private static string BuildRedactedDisplayUri(Uri endpointUri)
    {
        string port = endpointUri.IsDefaultPort
            ? string.Empty
            : ":" + endpointUri.Port.ToString(CultureInfo.InvariantCulture);
        string query = string.IsNullOrEmpty(endpointUri.Query)
            ? string.Empty
            : "?" + RedactedQueryMarker;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{endpointUri.Scheme}://{endpointUri.Host}{port}{NormalizeEndpointPath(endpointUri)}{query}");
    }
}