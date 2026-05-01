namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionOptions
{
    public const string DevFakeUploadFileEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_FILE_ENABLED";

    public const string DevFakeUploadHashEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_HASH_ENABLED";

    public const string DevFakeBusinessObjectKeyEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_BUSINESS_OBJECT_KEY_ENABLED";

    public const string DevFakePreUploadCheckEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_PREUPLOAD_CHECK_ENABLED";

    private DesktopUploadSectionOptions(
        bool isDevFakeUploadFileEnabled,
        bool isDevFakeUploadHashEnabled,
        bool isDevFakeBusinessObjectKeyEnabled,
        bool isDevFakePreUploadCheckEnabled)
    {
        IsDevFakeUploadFileEnabled = isDevFakeUploadFileEnabled;
        IsDevFakeUploadHashEnabled = isDevFakeUploadHashEnabled;
        IsDevFakeBusinessObjectKeyEnabled = isDevFakeBusinessObjectKeyEnabled;
        IsDevFakePreUploadCheckEnabled = isDevFakePreUploadCheckEnabled;
    }

    public static DesktopUploadSectionOptions Disabled { get; } = new(
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false);

#if DEBUG
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection { get; } = new(
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash { get; } = new(
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: true,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeBusinessObjectKey { get; } = new(
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: true,
        isDevFakePreUploadCheckEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakePreUploadCheck { get; } = new(
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: true);
#else
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeBusinessObjectKey => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakePreUploadCheck => Disabled;
#endif

    public bool IsDevFakeUploadFileEnabled { get; }

    public bool IsDevFakeUploadHashEnabled { get; }

    public bool IsDevFakeBusinessObjectKeyEnabled { get; }

    public bool IsDevFakePreUploadCheckEnabled { get; }

    public static DesktopUploadSectionOptions FromEnvironment()
    {
        return FromEnvironmentValue(
            Environment.GetEnvironmentVariable(DevFakeUploadFileEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeUploadHashEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeBusinessObjectKeyEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakePreUploadCheckEnabledEnvironmentVariable));
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(string? devFakeUploadFileEnabled)
    {
        return FromEnvironmentValue(
            devFakeUploadFileEnabled,
            devFakeUploadHashEnabled: null);
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(
        string? devFakeUploadFileEnabled,
        string? devFakeUploadHashEnabled)
    {
        return FromEnvironmentValue(
            devFakeUploadFileEnabled,
            devFakeUploadHashEnabled,
            devFakeBusinessObjectKeyEnabled: null);
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(
        string? devFakeUploadFileEnabled,
        string? devFakeUploadHashEnabled,
        string? devFakeBusinessObjectKeyEnabled)
    {
        return FromEnvironmentValue(
            devFakeUploadFileEnabled,
            devFakeUploadHashEnabled,
            devFakeBusinessObjectKeyEnabled,
            devFakePreUploadCheckEnabled: null);
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(
        string? devFakeUploadFileEnabled,
        string? devFakeUploadHashEnabled,
        string? devFakeBusinessObjectKeyEnabled,
        string? devFakePreUploadCheckEnabled)
    {
#if DEBUG
        bool isFakeFileEnabled = IsDevFakeUploadFileEnabledValue(devFakeUploadFileEnabled);
        bool isFakeHashEnabled = IsDevFakeUploadHashEnabledValue(devFakeUploadHashEnabled);
        bool isFakeBusinessObjectKeyEnabled =
            IsDevFakeBusinessObjectKeyEnabledValue(devFakeBusinessObjectKeyEnabled);
        bool isFakePreUploadCheckEnabled = IsDevFakePreUploadCheckEnabledValue(devFakePreUploadCheckEnabled);

        return new DesktopUploadSectionOptions(
            isFakeFileEnabled,
            isFakeHashEnabled,
            isFakeBusinessObjectKeyEnabled,
            isFakePreUploadCheckEnabled);
#else
        return Disabled;
#endif
    }

    public override string ToString()
    {
        return $"{nameof(DesktopUploadSectionOptions)} {{ IsDevFakeUploadFileEnabled = {IsDevFakeUploadFileEnabled}, "
            + $"IsDevFakeUploadHashEnabled = {IsDevFakeUploadHashEnabled}, "
            + $"IsDevFakeBusinessObjectKeyEnabled = {IsDevFakeBusinessObjectKeyEnabled}, "
            + $"IsDevFakePreUploadCheckEnabled = {IsDevFakePreUploadCheckEnabled} }}";
    }

    private static bool IsDevFakeUploadFileEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevFakeUploadHashEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevFakeBusinessObjectKeyEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevFakePreUploadCheckEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}