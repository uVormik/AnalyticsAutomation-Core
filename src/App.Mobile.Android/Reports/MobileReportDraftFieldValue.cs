namespace App.Mobile.Android.Reports;

internal sealed record MobileReportDraftFieldValue(
    string FieldKey,
    string Label,
    string ValueText,
    bool IsRequired,
    bool IsPlaceholder)
{
    public string SectionKey { get; init; } = string.Empty;

    public global::App.Mobile.Android.Lookup.MobileLookupFieldKind FieldKind { get; init; } =
        global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Text;

    public global::App.Mobile.Android.Lookup.MobileLookupSelectorMode SelectorMode { get; init; } =
        global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None;

    public DateTimeOffset? LastUpdatedAtUtc { get; init; }
}