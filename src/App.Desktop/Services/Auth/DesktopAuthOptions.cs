namespace App.Desktop.Services.Auth;

public sealed class DesktopAuthOptions
{
    public const string ControlPlaneBaseAddressEnvironmentVariable =
        "AA_DESKTOP_CONTROL_PLANE_BASE_ADDRESS";

    private DesktopAuthOptions(Uri? controlPlaneBaseAddress)
    {
        ControlPlaneBaseAddress = controlPlaneBaseAddress;
    }

    public static DesktopAuthOptions Disabled { get; } = new(controlPlaneBaseAddress: null);

    public Uri? ControlPlaneBaseAddress { get; }

    public bool IsControlPlaneSignInConfigured => ControlPlaneBaseAddress is not null;

    public static DesktopAuthOptions FromEnvironment()
    {
        return FromControlPlaneBaseAddress(
            Environment.GetEnvironmentVariable(ControlPlaneBaseAddressEnvironmentVariable));
    }

    public static DesktopAuthOptions FromControlPlaneBaseAddress(string? baseAddress)
    {
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            return Disabled;
        }

        return Uri.TryCreate(baseAddress.Trim(), UriKind.Absolute, out var parsed)
            ? FromControlPlaneBaseAddress(parsed)
            : Disabled;
    }

    public static DesktopAuthOptions FromControlPlaneBaseAddress(Uri? baseAddress)
    {
        if (baseAddress is null || !IsSafeControlPlaneBaseAddress(baseAddress))
        {
            return Disabled;
        }

        return new DesktopAuthOptions(baseAddress);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopAuthOptions)} {{ IsControlPlaneSignInConfigured = {IsControlPlaneSignInConfigured}, "
            + $"Scheme = {ControlPlaneBaseAddress?.Scheme ?? "<none>"}, Host = {ControlPlaneBaseAddress?.Host ?? "<none>"} }}";
    }

    private static bool IsSafeControlPlaneBaseAddress(Uri baseAddress)
    {
        if (!baseAddress.IsAbsoluteUri || string.IsNullOrWhiteSpace(baseAddress.Host))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(baseAddress.UserInfo))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(baseAddress.Query) || !string.IsNullOrEmpty(baseAddress.Fragment))
        {
            return false;
        }

        if (baseAddress.AbsolutePath is not "" and not "/")
        {
            return false;
        }

        return baseAddress.Scheme == Uri.UriSchemeHttps
            || baseAddress.Scheme == Uri.UriSchemeHttp;
    }
}