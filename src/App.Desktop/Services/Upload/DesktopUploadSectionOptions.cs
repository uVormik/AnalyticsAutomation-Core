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

    public const string DevFakeSiteUploadEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_SITE_UPLOAD_ENABLED";

    public const string DevFakeUploadReceiptEnabledEnvironmentVariable =
        "AA_DESKTOP_DEV_FAKE_UPLOAD_RECEIPT_ENABLED";

    private DesktopUploadSectionOptions(
        bool isLiveControlPlanePreUploadCheckEnabled,
        bool isLiveControlPlaneUploadReceiptEnabled,
        bool isDevFakeUploadFileEnabled,
        bool isDevFakeUploadHashEnabled,
        bool isDevFakeBusinessObjectKeyEnabled,
        bool isDevFakePreUploadCheckEnabled,
        bool isDevFakeSiteUploadEnabled,
        bool isDevFakeUploadReceiptEnabled)
    {
        IsLiveControlPlanePreUploadCheckEnabled = isLiveControlPlanePreUploadCheckEnabled;
        IsLiveControlPlaneUploadReceiptEnabled = isLiveControlPlaneUploadReceiptEnabled;
        IsDevFakeUploadFileEnabled = isDevFakeUploadFileEnabled;
        IsDevFakeUploadHashEnabled = isDevFakeUploadHashEnabled;
        IsDevFakeBusinessObjectKeyEnabled = isDevFakeBusinessObjectKeyEnabled;
        IsDevFakePreUploadCheckEnabled = isDevFakePreUploadCheckEnabled;
        IsDevFakeSiteUploadEnabled = isDevFakeSiteUploadEnabled;
        IsDevFakeUploadReceiptEnabled = isDevFakeUploadReceiptEnabled;
    }

    public static DesktopUploadSectionOptions Disabled { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false,
        isDevFakeSiteUploadEnabled: false,
        isDevFakeUploadReceiptEnabled: false);

#if DEBUG
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false,
        isDevFakeSiteUploadEnabled: false,
        isDevFakeUploadReceiptEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: true,
        isDevFakeUploadHashEnabled: true,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false,
        isDevFakeSiteUploadEnabled: false,
        isDevFakeUploadReceiptEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeBusinessObjectKey { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: true,
        isDevFakePreUploadCheckEnabled: false,
        isDevFakeSiteUploadEnabled: false,
        isDevFakeUploadReceiptEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakePreUploadCheck { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: true,
        isDevFakeSiteUploadEnabled: false,
        isDevFakeUploadReceiptEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeSiteUpload { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false,
        isDevFakeSiteUploadEnabled: true,
        isDevFakeUploadReceiptEnabled: false);

    public static DesktopUploadSectionOptions EnabledForDevFakeUploadReceipt { get; } = new(
        isLiveControlPlanePreUploadCheckEnabled: false,
        isLiveControlPlaneUploadReceiptEnabled: false,
        isDevFakeUploadFileEnabled: false,
        isDevFakeUploadHashEnabled: false,
        isDevFakeBusinessObjectKeyEnabled: false,
        isDevFakePreUploadCheckEnabled: false,
        isDevFakeSiteUploadEnabled: false,
        isDevFakeUploadReceiptEnabled: true);
#else
    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelection => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeFileSelectionAndHash => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeBusinessObjectKey => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakePreUploadCheck => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeSiteUpload => Disabled;

    public static DesktopUploadSectionOptions EnabledForDevFakeUploadReceipt => Disabled;
#endif

    public bool IsLiveControlPlanePreUploadCheckEnabled { get; }

    public bool IsLiveControlPlaneUploadReceiptEnabled { get; }

    public bool IsDevFakeUploadFileEnabled { get; }

    public bool IsDevFakeUploadHashEnabled { get; }

    public bool IsDevFakeBusinessObjectKeyEnabled { get; }

    public bool IsDevFakePreUploadCheckEnabled { get; }

    public bool IsDevFakeSiteUploadEnabled { get; }

    public bool IsDevFakeUploadReceiptEnabled { get; }

    public static DesktopUploadSectionOptions FromEnvironment()
    {
        return FromEnvironmentValue(
            Environment.GetEnvironmentVariable(DevFakeUploadFileEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeUploadHashEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeBusinessObjectKeyEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakePreUploadCheckEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeSiteUploadEnabledEnvironmentVariable),
            Environment.GetEnvironmentVariable(DevFakeUploadReceiptEnabledEnvironmentVariable));
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
        return FromEnvironmentValue(
            devFakeUploadFileEnabled,
            devFakeUploadHashEnabled,
            devFakeBusinessObjectKeyEnabled,
            devFakePreUploadCheckEnabled,
            devFakeSiteUploadEnabled: null);
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(
        string? devFakeUploadFileEnabled,
        string? devFakeUploadHashEnabled,
        string? devFakeBusinessObjectKeyEnabled,
        string? devFakePreUploadCheckEnabled,
        string? devFakeSiteUploadEnabled)
    {
        return FromEnvironmentValue(
            devFakeUploadFileEnabled,
            devFakeUploadHashEnabled,
            devFakeBusinessObjectKeyEnabled,
            devFakePreUploadCheckEnabled,
            devFakeSiteUploadEnabled,
            devFakeUploadReceiptEnabled: null);
    }

    public static DesktopUploadSectionOptions FromEnvironmentValue(
        string? devFakeUploadFileEnabled,
        string? devFakeUploadHashEnabled,
        string? devFakeBusinessObjectKeyEnabled,
        string? devFakePreUploadCheckEnabled,
        string? devFakeSiteUploadEnabled,
        string? devFakeUploadReceiptEnabled)
    {
#if DEBUG
        bool isFakeFileEnabled = IsDevFakeUploadFileEnabledValue(devFakeUploadFileEnabled);
        bool isFakeHashEnabled = IsDevFakeUploadHashEnabledValue(devFakeUploadHashEnabled);
        bool isFakeBusinessObjectKeyEnabled =
            IsDevFakeBusinessObjectKeyEnabledValue(devFakeBusinessObjectKeyEnabled);
        bool isFakePreUploadCheckEnabled = IsDevFakePreUploadCheckEnabledValue(devFakePreUploadCheckEnabled);
        bool isFakeSiteUploadEnabled = IsDevFakeSiteUploadEnabledValue(devFakeSiteUploadEnabled);
        bool isFakeUploadReceiptEnabled = IsDevFakeUploadReceiptEnabledValue(devFakeUploadReceiptEnabled);

        return new DesktopUploadSectionOptions(
            isLiveControlPlanePreUploadCheckEnabled: false,
            isLiveControlPlaneUploadReceiptEnabled: false,
            isDevFakeUploadFileEnabled: isFakeFileEnabled,
            isDevFakeUploadHashEnabled: isFakeHashEnabled,
            isDevFakeBusinessObjectKeyEnabled: isFakeBusinessObjectKeyEnabled,
            isDevFakePreUploadCheckEnabled: isFakePreUploadCheckEnabled,
            isDevFakeSiteUploadEnabled: isFakeSiteUploadEnabled,
            isDevFakeUploadReceiptEnabled: isFakeUploadReceiptEnabled);
#else
        return Disabled;
#endif
    }

    public DesktopUploadSectionOptions WithLiveControlPlanePreUploadCheck()
    {
        return new DesktopUploadSectionOptions(
            isLiveControlPlanePreUploadCheckEnabled: true,
            isLiveControlPlaneUploadReceiptEnabled: IsLiveControlPlaneUploadReceiptEnabled,
            isDevFakeUploadFileEnabled: IsDevFakeUploadFileEnabled,
            isDevFakeUploadHashEnabled: IsDevFakeUploadHashEnabled,
            isDevFakeBusinessObjectKeyEnabled: IsDevFakeBusinessObjectKeyEnabled,
            isDevFakePreUploadCheckEnabled: false,
            isDevFakeSiteUploadEnabled: IsDevFakeSiteUploadEnabled,
            isDevFakeUploadReceiptEnabled: IsDevFakeUploadReceiptEnabled);
    }

    public DesktopUploadSectionOptions WithLiveControlPlaneUploadReceipt()
    {
        return new DesktopUploadSectionOptions(
            isLiveControlPlanePreUploadCheckEnabled: IsLiveControlPlanePreUploadCheckEnabled,
            isLiveControlPlaneUploadReceiptEnabled: true,
            isDevFakeUploadFileEnabled: IsDevFakeUploadFileEnabled,
            isDevFakeUploadHashEnabled: IsDevFakeUploadHashEnabled,
            isDevFakeBusinessObjectKeyEnabled: IsDevFakeBusinessObjectKeyEnabled,
            isDevFakePreUploadCheckEnabled: IsDevFakePreUploadCheckEnabled,
            isDevFakeSiteUploadEnabled: IsDevFakeSiteUploadEnabled,
            isDevFakeUploadReceiptEnabled: false);
    }

    public DesktopUploadSectionOptions WithoutDevFakeSiteUpload()
    {
        return new DesktopUploadSectionOptions(
            isLiveControlPlanePreUploadCheckEnabled: IsLiveControlPlanePreUploadCheckEnabled,
            isLiveControlPlaneUploadReceiptEnabled: IsLiveControlPlaneUploadReceiptEnabled,
            isDevFakeUploadFileEnabled: IsDevFakeUploadFileEnabled,
            isDevFakeUploadHashEnabled: IsDevFakeUploadHashEnabled,
            isDevFakeBusinessObjectKeyEnabled: IsDevFakeBusinessObjectKeyEnabled,
            isDevFakePreUploadCheckEnabled: IsDevFakePreUploadCheckEnabled,
            isDevFakeSiteUploadEnabled: false,
            isDevFakeUploadReceiptEnabled: IsDevFakeUploadReceiptEnabled);
    }

    public override string ToString()
    {
        return $"{nameof(DesktopUploadSectionOptions)} {{ "
            + $"IsLiveControlPlanePreUploadCheckEnabled = {IsLiveControlPlanePreUploadCheckEnabled}, "
            + $"IsLiveControlPlaneUploadReceiptEnabled = {IsLiveControlPlaneUploadReceiptEnabled}, "
            + $"IsDevFakeUploadFileEnabled = {IsDevFakeUploadFileEnabled}, "
            + $"IsDevFakeUploadHashEnabled = {IsDevFakeUploadHashEnabled}, "
            + $"IsDevFakeBusinessObjectKeyEnabled = {IsDevFakeBusinessObjectKeyEnabled}, "
            + $"IsDevFakePreUploadCheckEnabled = {IsDevFakePreUploadCheckEnabled}, "
            + $"IsDevFakeSiteUploadEnabled = {IsDevFakeSiteUploadEnabled}, "
            + $"IsDevFakeUploadReceiptEnabled = {IsDevFakeUploadReceiptEnabled} }}";
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

    private static bool IsDevFakeSiteUploadEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevFakeUploadReceiptEnabledValue(string? value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}