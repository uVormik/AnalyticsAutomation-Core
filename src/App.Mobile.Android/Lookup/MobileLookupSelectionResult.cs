namespace App.Mobile.Android.Lookup;

internal sealed record MobileLookupSelectionResult(
    bool Applied,
    string FieldKey,
    string? SelectedDisplayText,
    string Message);