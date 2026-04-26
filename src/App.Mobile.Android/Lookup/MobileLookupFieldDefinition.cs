namespace App.Mobile.Android.Lookup;

internal sealed record MobileLookupFieldDefinition(
    string FieldKey,
    string Label,
    MobileLookupFieldKind Kind,
    MobileLookupSelectorMode SelectorMode,
    bool IsRequired,
    string SectionKey,
    string PlaceholderText);