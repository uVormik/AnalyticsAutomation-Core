namespace App.Desktop.Services.Auth;

public sealed class DesktopAuthOptions
{
    public const string ControlPlaneBaseAddressEnvironmentVariable =
        "AA_DESKTOP_CONTROL_PLANE_BASE_ADDRESS";

    public const string DevFakeAuthEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_AUTH_ENABLED";

    private DesktopAuthOptions(
        Uri? controlPlaneBaseAddress,
        bool isDevFakeAuthEnabled)
    {
        ControlPlaneBaseAddress = controlPlaneBaseAddress;
        IsDevFakeAuthEnabled = isDevFakeAuthEnabled;
    }

    public static DesktopAuthOptions Disabled { get; } = new(
        controlPlaneBaseAddress: null,
        isDevFakeAuthEnabled: false);

    public Uri? ControlPlaneBaseAddress { get; }

    public bool IsControlPlaneSignInConfigured => ControlPlaneBaseAddress is not null;

    public bool IsDevFakeAuthEnabled { get; }

    public static DesktopAuthOptions FromEnvironment()
    {
        return FromEnvironmentValues(
            Environment.GetEnvironmentVariable(ControlPlaneBaseAddressEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeAuthEnabledEnvironmentVariable));
    }

    public static DesktopAuthOptions FromEnvironmentValues(
        string? baseAddress,
        string? devFakeAuthEnabled)
    {
        DesktopAuthOptions liveOptions = FromControlPlaneBaseAddress(baseAddress);

#if DEBUG
        if (!liveOptions.IsControlPlaneSignInConfigured
            && IsDevFakeAuthEnabledValue(devFakeAuthEnabled))
        {
            return new DesktopAuthOptions(
                controlPlaneBaseAddress: null,
                isDevFakeAuthEnabled: true);
        }
#endif

        return liveOptions;
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

        return new DesktopAuthOptions(
            baseAddress,
            isDevFakeAuthEnabled: false);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopAuthOptions)} {{ IsControlPlaneSignInConfigured = {IsControlPlaneSignInConfigured}, "
            + $"IsDevFakeAuthEnabled = {IsDevFakeAuthEnabled}, "
            + $"Scheme = {ControlPlaneBaseAddress?.Scheme ?? "<none>"}, Host = {ControlPlaneBaseAddress?.Host ?? "<none>"} }}";
    }

    private static bool IsDevFakeAuthEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
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