namespace App.Mobile.Android.Lookup;

internal sealed record MobileLookupCatalogSnapshot(
    string SnapshotId,
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<MobileLookupFieldDefinition> Fields,
    IReadOnlyDictionary<string, IReadOnlyList<MobileLookupOption>> OptionsByFieldKey);