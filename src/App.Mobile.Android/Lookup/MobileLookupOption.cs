namespace App.Mobile.Android.Lookup;

internal sealed record MobileLookupOption(
    string OptionKey,
    string DisplayText,
    int SortOrder,
    bool IsAvailable);