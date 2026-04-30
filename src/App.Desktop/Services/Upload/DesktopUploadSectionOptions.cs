namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionOptions
{
    public const string DevFakeUploadFileEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_FILE_ENABLED";

    public const string DevFakeUploadHashEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_HASH_ENABLED";

    private DesktopUploadSectionOptions(
        bool isDevFakeUploadFileEnabled,
        bool isDevFakeUploadHashEnabled)
    {
        IsDevFakeUploadFileEnabled = isDevFakeUploadFileEnabled;
        IsDevFakeUploadHashEnabled = isDevFakeUploadHashEnabled;
    }

    public static DesktopUploadSectionOptions Disabled { get; } = new(
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false);

#if DEBUG
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection { get; } = new(
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash { get; } = new(
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: true);
#else
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash => Disabled;
#endif

    public bool IsDevFakeUploadFileEnabled { get; }

    public bool IsDevFakeUploadHashEnabled { get; }

    public static DesktopUploadSectionOptions FromEnvironment()
    {
        return FromEnvironmentValue(
            Environment.GetEnvironmentVariable(DevFakeUploadFileEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeUploadHashEnabledEnvironmentVariable));
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
#if DEBUG
        bool isFakeFileEnabled = IsDevFakeUploadFileEnabledValue(devFakeUploadFileEnabled);
        bool isFakeHashEnabled = IsDevFakeUploadHashEnabledValue(devFakeUploadHashEnabled);

        if (isFakeFileEnabled && isFakeHashEnabled)
        {
            return EnabledForDevFakeFileSelectionAndHash;
        }

        if (isFakeFileEnabled)
        {
            return EnabledForDevFakeFileSelection;
        }

        if (isFakeHashEnabled)
        {
            return new DesktopUploadSectionOptions(
                isDevFakeUploadFileEnabled: false,
                isDevFakeUploadHashEnabled: true);
        }
#endif

        return Disabled;
    }

    public override string ToString()
    {
        return $"{nameof(DesktopUploadSectionOptions)} {{ IsDevFakeUploadFileEnabled = {IsDevFakeUploadFileEnabled}, "
            + $"IsDevFakeUploadHashEnabled = {IsDevFakeUploadHashEnabled} }}";
    }

    private static bool IsDevFakeUploadFileEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevFakeUploadHashEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}