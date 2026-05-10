namespace App.Desktop.Services.Upload;

public sealed class DesktopDirectSiteProviderOptions
{
    public const string ProviderKeyEnvironmentVariable =
        "AA_DESKTOP_DIRECT_SITE_PROVIDER_KEY";

    public const string ProviderBaseAddressEnvironmentVariable =
        "AA_DESKTOP_DIRECT_SITE_PROVIDER_BASE_ADDRESS";

    private static readonly string[] TokenLikeProviderKeySegments =
    [
        "authorization",
        "credential",
        "password",
        "refresh",
        "secret",
        "token"
    ];

    private DesktopDirectSiteProviderOptions(
        bool isProviderConfigPresent,
        string? providerKey,
        Uri? providerBaseAddress)
    {
        IsProviderConfigPresent = isProviderConfigPresent;
        ProviderKey = providerKey;
        ProviderBaseAddress = providerBaseAddress;
    }

    public static DesktopDirectSiteProviderOptions Disabled { get; } = new(
        isProviderConfigPresent: false,
        providerKey: null,
        providerBaseAddress: null);

    public bool IsProviderConfigPresent { get; }

    public bool IsProviderConfigValid => ProviderKey is not null && ProviderBaseAddress is not null;

    public bool IsDirectSiteUploadAvailable { get; }

    public string? ProviderKey { get; }

    public Uri? ProviderBaseAddress { get; }

    public static DesktopDirectSiteProviderOptions FromEnvironment()
    {
        return FromEnvironmentValues(
            Environment.GetEnvironmentVariable(ProviderKeyEnvironmentVariable),
            Environment.GetEnvironmentVariable(ProviderBaseAddressEnvironmentVariable));
    }

    public static DesktopDirectSiteProviderOptions FromEnvironmentValues(
        string? providerKey,
        string? providerBaseAddress)
    {
        bool isProviderConfigPresent = !string.IsNullOrWhiteSpace(providerKey)
            || !string.IsNullOrWhiteSpace(providerBaseAddress);

        if (!isProviderConfigPresent)
        {
            return Disabled;
        }

        return new DesktopDirectSiteProviderOptions(
            isProviderConfigPresent,
            NormalizeProviderKey(providerKey),
            ParseProviderBaseAddress(providerBaseAddress));
    }

    public override string ToString()
    {
        return $"{nameof(DesktopDirectSiteProviderOptions)} {{ "
            + $"IsProviderConfigPresent = {IsProviderConfigPresent}, "
            + $"IsProviderConfigValid = {IsProviderConfigValid}, "
            + $"IsDirectSiteUploadAvailable = {IsDirectSiteUploadAvailable}, "
            + $"ProviderKey = {ProviderKey ?? "<none>"}, "
            + $"Scheme = {ProviderBaseAddress?.Scheme ?? "<none>"}, "
            + $"Host = {ProviderBaseAddress?.Host ?? "<none>"} }}";
    }

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

    private static Uri? ParseProviderBaseAddress(string? providerBaseAddress)
    {
        if (string.IsNullOrWhiteSpace(providerBaseAddress)
            || !Uri.TryCreate(providerBaseAddress.Trim(), UriKind.Absolute, out Uri? parsed)
            || !IsSafeProviderBaseAddress(parsed))
        {
            return null;
        }

        return parsed;
    }

    private static bool IsSafeProviderBaseAddress(Uri providerBaseAddress)
    {
        if (!providerBaseAddress.IsAbsoluteUri || string.IsNullOrWhiteSpace(providerBaseAddress.Host))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(providerBaseAddress.UserInfo))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(providerBaseAddress.Query) || !string.IsNullOrEmpty(providerBaseAddress.Fragment))
        {
            return false;
        }

        if (providerBaseAddress.AbsolutePath is not "" and not "/")
        {
            return false;
        }

        return providerBaseAddress.Scheme == Uri.UriSchemeHttps;
    }
}