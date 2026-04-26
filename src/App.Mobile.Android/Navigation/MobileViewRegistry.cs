namespace App.Mobile.Android.Navigation;

internal sealed class MobileViewRegistry
{
    public IReadOnlyList<MobileMenuEntry> MenuEntries { get; } =
        new[]
        {
            new MobileMenuEntry(
                MobileViewId.Reports,
                global::App.Mobile.Android.Localization.MobileUiText.MenuReports,
                "/reports",
                "bi bi-journal-text-nav-menu"),
            new MobileMenuEntry(
                MobileViewId.Queue,
                global::App.Mobile.Android.Localization.MobileUiText.MenuQueue,
                "/queue",
                "bi bi-list-nested-nav-menu"),
            new MobileMenuEntry(
                MobileViewId.Profile,
                "Профиль",
                "/profile",
                "bi bi-person-circle-nav-menu"),
            new MobileMenuEntry(
                MobileViewId.Home,
                global::App.Mobile.Android.Localization.MobileUiText.MenuHome,
                "/",
                "bi bi-house-door-fill-nav-menu"),
            new MobileMenuEntry(
                MobileViewId.Upload,
                global::App.Mobile.Android.Localization.MobileUiText.MenuUpload,
                "/upload",
                "bi bi-plus-square-fill-nav-menu")
        };

    public IReadOnlyList<MobileMenuEntry> GetVisibleMenuEntries(
        global::App.Mobile.Android.Services.Abstractions.IMobileAccessContext accessContext)
    {
        var visibleEntries = new List<MobileMenuEntry>();

        foreach (var entry in MenuEntries)
        {
            if (accessContext.CanAccess(entry.ViewId))
            {
                visibleEntries.Add(entry);
            }
        }

        return visibleEntries;
    }
}