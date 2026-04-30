namespace App.Desktop.Services.Upload;

public sealed class DesktopUploadSectionOptions
{
    public const string DevFakeUploadFileEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_FILE_ENABLED";

    private DesktopUploadSectionOptions(bool isDevFakeUploadFileEnabled)
    {
        IsDevFakeUploadFileEnabled = isDevFakeUploadFileEnabled;
    }

    public static DesktopUploadSectionOptions Disabled { get; } = new(
        isDevFakeUploadFileEnabled: false);

#if DEBUG
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection { get; } = new(
        isDevFakeUploadFileEnabled: true);
#else
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection => Disabled;
#endif

    public bool IsDevFakeUploadFileEnabled { get; }

    public static DesktopUploadSectionOptions FromEnvironment()
    {
        return FromEnvironmentValue(
            Environment.GetEnvironmentVariable(DevFakeUploadFileEnabledEnvironmentVariable));
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(string? devFakeUploadFileEnabled)
    {
#if DEBUG
        if (IsDevFakeUploadFileEnabledValue(devFakeUploadFileEnabled))
        {
            return EnabledForDevFakeFileSelection;
        }
#endif

        return Disabled;
    }

    public override string ToString()
    {
        return $"{nameof(DesktopUploadSectionOptions)} {{ IsDevFakeUploadFileEnabled = {IsDevFakeUploadFileEnabled} }}";
    }

    private static bool IsDevFakeUploadFileEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}