namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionOptions
{
    public const string DevFakeUploadFileEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_FILE_ENABLED";

    public const string DevFakeUploadHashEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_HASH_ENABLED";

    public const string DevFakeBusinessObjectKeyEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_BUSINESS_OBJECT_KEY_ENABLED";

    private DesktopUploadSectionOptions(
        bool isDevFakeUploadFileEnabled,
        bool isDevFakeUploadHashEnabled,
        bool isDevFakeBusinessObjectKeyEnabled)
    {
        IsDevFakeUploadFileEnabled = isDevFakeUploadFileEnabled;
        IsDevFakeUploadHashEnabled = isDevFakeUploadHashEnabled;
        IsDevFakeBusinessObjectKeyEnabled = isDevFakeBusinessObjectKeyEnabled;
    }

    public static DesktopUploadSectionOptions Disabled { get; } = new(
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false);

#if DEBUG
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection { get; } = new(
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash { get; } = new(
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: true,
        isDevFakeBusinessObjectKeyEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeBusinessObjectKey { get; } = new(
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: true);
#else
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeBusinessObjectKey => Disabled;
#endif

    public bool IsDevFakeUploadFileEnabled { get; }

    public bool IsDevFakeUploadHashEnabled { get; }

    public bool IsDevFakeBusinessObjectKeyEnabled { get; }

    public static DesktopUploadSectionOptions FromEnvironment()
    {
        return FromEnvironmentValue(
            Environment.GetEnvironmentVariable(DevFakeUploadFileEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeUploadHashEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeBusinessObjectKeyEnabledEnvironmentVariable));
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
#if DEBUG
        bool isFakeFileEnabled = IsDevFakeUploadFileEnabledValue(devFakeUploadFileEnabled);
        bool isFakeHashEnabled = IsDevFakeUploadHashEnabledValue(devFakeUploadHashEnabled);
        bool isFakeBusinessObjectKeyEnabled =
            IsDevFakeBusinessObjectKeyEnabledValue(devFakeBusinessObjectKeyEnabled);

        return new DesktopUploadSectionOptions(
            isFakeFileEnabled,
            isFakeHashEnabled,
            isFakeBusinessObjectKeyEnabled);
#else
        return Disabled;
#endif
    }

    public override string ToString()
    {
        return $"{nameof(DesktopUploadSectionOptions)} {{ IsDevFakeUploadFileEnabled = {IsDevFakeUploadFileEnabled}, "
            + $"IsDevFakeUploadHashEnabled = {IsDevFakeUploadHashEnabled}, "
            + $"IsDevFakeBusinessObjectKeyEnabled = {IsDevFakeBusinessObjectKeyEnabled} }}";
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
}