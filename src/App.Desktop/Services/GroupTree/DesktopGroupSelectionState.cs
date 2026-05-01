using App.Desktop.Boundaries;

namespace App.Desktop.Services.GroupTree;

public sealed class DesktopGroupSelectionState
{
    public DesktopSelectedGroupContext? SelectedGroup { get; private set; }

    public bool TrySelect(DesktopGroupTreeNode? node)
    {
        if (node?.CanSelect != true
            || !TryCreateSafeId(node.Id, out string safeId)
            || !TryCreateSafeDisplayName(node.DisplayName, out string safeDisplayName))
        {
            return false;
        }

        SelectedGroup = new DesktopSelectedGroupContext(safeId, safeDisplayName);
        return true;
    }

    public void Clear()
    {
        SelectedGroup = null;
    }

    private static bool TryCreateSafeId(string? value, out string safeValue)
    {
        safeValue = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 80)
        {
            return false;
        }

        foreach (char character in trimmed)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')
            {
                return false;
            }
        }

        safeValue = trimmed;
        return true;
    }

    private static bool TryCreateSafeDisplayName(string? value, out string safeValue)
    {
        safeValue = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 80)
        {
            return false;
        }

        foreach (char character in trimmed)
        {
            if (char.IsControl(character))
            {
                return false;
            }
        }

        string lower = trimmed.ToLowerInvariant();
        string[] blockedFragments =
        [
            "authorization",
            "bearer",
            "password",
            "token",
            "sessionid",
            "session_id",
            "access_token",
            "refresh_token",
            "accesstoken",
            "refreshtoken"
        ];

        foreach (string blockedFragment in blockedFragments)
        {
            if (lower.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        safeValue = trimmed;
        return true;
    }
}