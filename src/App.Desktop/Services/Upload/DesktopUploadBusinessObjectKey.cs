using App.Desktop.Boundaries;

namespace App.Desktop.Services.Upload;

public sealed record DesktopUploadBusinessObjectKey(string Value)
{
    public const int MaxLength = 128;
    public const string VisualSmokeValue = "visual-smoke-business-object-001";
}

public sealed record DesktopUploadBusinessObjectKeyValidation(
    bool IsValid,
    DesktopUploadBusinessObjectKey? BusinessObjectKey,
    string SafeInputValue,
    string Message);

public static class DesktopUploadBusinessObjectKeyValidator
{
    private static readonly string[] BlockedFragments =
    [
        "authorization",
        "bearer",
        "password",
        "token",
        "sessionid",
        "session_id",
        "access_token",
        "accesstoken",
        "refresh_token",
        "refreshtoken"
    ];

    public static DesktopUploadBusinessObjectKeyValidation ValidateManualInput(string? value)
    {
        string safeInputValue = CreateSafeSingleLineInput(value);

        if (string.IsNullOrEmpty(safeInputValue))
        {
            return Invalid(safeInputValue, DesktopUploadSectionText.BusinessObjectKeyEmptyValidationMessage);
        }

        if (ContainsControlCharacter(value))
        {
            return Invalid(
                safeInputValue,
                DesktopUploadSectionText.BusinessObjectKeyControlCharacterValidationMessage);
        }

        if (safeInputValue.Length > DesktopUploadBusinessObjectKey.MaxLength)
        {
            return Invalid(
                safeInputValue[..DesktopUploadBusinessObjectKey.MaxLength],
                DesktopUploadSectionText.BusinessObjectKeyLengthValidationMessage);
        }

        if (ContainsBlockedFragment(safeInputValue))
        {
            return Invalid(
                string.Empty,
                DesktopUploadSectionText.BusinessObjectKeySecretValidationMessage);
        }

        var businessObjectKey = new DesktopUploadBusinessObjectKey(safeInputValue);
        return new DesktopUploadBusinessObjectKeyValidation(
            IsValid: true,
            businessObjectKey,
            safeInputValue,
            DesktopUploadSectionText.BusinessObjectKeyAcceptedMessage);
    }

    private static DesktopUploadBusinessObjectKeyValidation Invalid(string safeInputValue, string message)
    {
        return new DesktopUploadBusinessObjectKeyValidation(
            IsValid: false,
            BusinessObjectKey: null,
            safeInputValue,
            message);
    }

    private static string CreateSafeSingleLineInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var safeCharacters = new char[value.Length];
        var safeLength = 0;

        foreach (char character in value.Trim())
        {
            if (char.IsControl(character))
            {
                continue;
            }

            safeCharacters[safeLength] = character;
            safeLength++;
        }

        return new string(safeCharacters, 0, safeLength).Trim();
    }

    private static bool ContainsControlCharacter(string? value)
    {
        if (value is null)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsBlockedFragment(string value)
    {
        string lower = value.ToLowerInvariant();
        foreach (string blockedFragment in BlockedFragments)
        {
            if (lower.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}