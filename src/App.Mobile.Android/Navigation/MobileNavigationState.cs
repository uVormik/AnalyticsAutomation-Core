namespace App.Mobile.Android.Navigation;

internal sealed class MobileNavigationState
{
    public MobileViewId CurrentViewId { get; private set; } = MobileViewId.Reports;

    public void SetCurrentView(MobileViewId viewId)
    {
        CurrentViewId = viewId;
    }
}