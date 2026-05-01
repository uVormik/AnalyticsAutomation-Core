namespace App.Desktop.Services.GroupTree;

public sealed class DesktopGroupTreeOptions
{
    public const string DevFakeGroupTreeEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_GROUP_TREE_ENABLED";

    private DesktopGroupTreeOptions(bool isDevFakeGroupTreeEnabled)
    {
        IsDevFakeGroupTreeEnabled = isDevFakeGroupTreeEnabled;
    }

    public static DesktopGroupTreeOptions Disabled { get; } = new(isDevFakeGroupTreeEnabled: false);

#if DEBUG
    public static DesktopGroupTreeOptions EnabledForDevFakeGroupTree { get; } = new(
        isDevFakeGroupTreeEnabled: true);
#else
    public static DesktopGroupTreeOptions EnabledForDevFakeGroupTree => Disabled;
#endif

    public bool IsDevFakeGroupTreeEnabled { get; }

    public static DesktopGroupTreeOptions FromEnvironment()
    {
        return FromEnvironmentValue(
            Environment.GetEnvironmentVariable(DevFakeGroupTreeEnabledEnvironmentVariable));
    }

    public static DesktopGroupTreeOptions FromEnvironmentValue(string? devFakeGroupTreeEnabled)
    {
#if DEBUG
        return new DesktopGroupTreeOptions(IsDevFakeGroupTreeEnabledValue(devFakeGroupTreeEnabled));
#else
        return Disabled;
#endif
    }

    public override string ToString()
    {
        return $"{nameof(DesktopGroupTreeOptions)} {{ IsDevFakeGroupTreeEnabled = {IsDevFakeGroupTreeEnabled} }}";
    }

    private static bool IsDevFakeGroupTreeEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}