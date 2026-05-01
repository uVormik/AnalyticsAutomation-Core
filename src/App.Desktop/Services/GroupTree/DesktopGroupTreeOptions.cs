using App.Desktop.Services.Auth;

namespace App.Desktop.Services.GroupTree;

public sealed class DesktopGroupTreeOptions
{
    public const string ControlPlaneBaseAddressEnvironmentVariable =
        DesktopAuthOptions.ControlPlaneBaseAddressEnvironmentVariable;

    public const string DevFakeGroupTreeEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_GROUP_TREE_ENABLED";

    private DesktopGroupTreeOptions(
        bool isLiveControlPlaneGroupTreeEnabled,
        bool isDevFakeGroupTreeEnabled)
    {
        IsLiveControlPlaneGroupTreeEnabled = isLiveControlPlaneGroupTreeEnabled;
        IsDevFakeGroupTreeEnabled = isDevFakeGroupTreeEnabled;
    }

    public static DesktopGroupTreeOptions Disabled { get; } = new(
        isLiveControlPlaneGroupTreeEnabled: false,
        isDevFakeGroupTreeEnabled: false);

    public static DesktopGroupTreeOptions EnabledForLiveControlPlane { get; } = new(
        isLiveControlPlaneGroupTreeEnabled: true,
        isDevFakeGroupTreeEnabled: false);

#if DEBUG
    public static DesktopGroupTreeOptions EnabledForDevFakeGroupTree { get; } = new(
        isLiveControlPlaneGroupTreeEnabled: false,
        isDevFakeGroupTreeEnabled: true);
#else
    public static DesktopGroupTreeOptions EnabledForDevFakeGroupTree => Disabled;
#endif

    public bool IsLiveControlPlaneGroupTreeEnabled { get; }

    public bool IsDevFakeGroupTreeEnabled { get; }

    public bool IsGroupTreeClientConfigured =>
        IsLiveControlPlaneGroupTreeEnabled || IsDevFakeGroupTreeEnabled;

    public static DesktopGroupTreeOptions FromEnvironment()
    {
        return FromEnvironmentValues(
            Environment.GetEnvironmentVariable(ControlPlaneBaseAddressEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeGroupTreeEnabledEnvironmentVariable));
    }

    public static DesktopGroupTreeOptions FromEnvironmentValue(string? devFakeGroupTreeEnabled)
    {
        return FromEnvironmentValues(
            controlPlaneBaseAddress: null,
            devFakeGroupTreeEnabled);
    }

    public static DesktopGroupTreeOptions FromEnvironmentValues(
        string? controlPlaneBaseAddress,
        string? devFakeGroupTreeEnabled)
    {
        DesktopAuthOptions authOptions =
            DesktopAuthOptions.FromControlPlaneBaseAddress(controlPlaneBaseAddress);

        return FromAuthOptions(authOptions, devFakeGroupTreeEnabled);
    }

    public static DesktopGroupTreeOptions FromAuthOptions(DesktopAuthOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(authOptions);

        return FromAuthOptions(
            authOptions,
            Environment.GetEnvironmentVariable(DevFakeGroupTreeEnabledEnvironmentVariable));
    }

    public static DesktopGroupTreeOptions FromAuthOptions(
        DesktopAuthOptions authOptions,
        string? devFakeGroupTreeEnabled)
    {
        ArgumentNullException.ThrowIfNull(authOptions);

        if (authOptions.IsControlPlaneSignInConfigured)
        {
            return EnabledForLiveControlPlane;
        }

#if DEBUG
        if (IsDevFakeGroupTreeEnabledValue(devFakeGroupTreeEnabled))
        {
            return EnabledForDevFakeGroupTree;
        }
#else
        _ = devFakeGroupTreeEnabled;
#endif

        return Disabled;
    }

    public override string ToString()
    {
        return $"{nameof(DesktopGroupTreeOptions)} {{ "
            + $"IsLiveControlPlaneGroupTreeEnabled = {IsLiveControlPlaneGroupTreeEnabled}, "
            + $"IsDevFakeGroupTreeEnabled = {IsDevFakeGroupTreeEnabled} }}";
    }

    private static bool IsDevFakeGroupTreeEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}