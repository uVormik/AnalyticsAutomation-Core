namespace App.Mobile.Android.Services.Stubs;

internal sealed class StubMobileReportLookupProvider :
    global::App.Mobile.Android.Services.Abstractions.IMobileLookupCatalogProvider
{
    private static readonly global::App.Mobile.Android.Lookup.MobileLookupFieldDefinition[] FieldDefinitions =
    [
        new(
            FieldKey: "device_type",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeviceTypeLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: true,
            SectionKey: "basic-data",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "serial_number",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldSerialNumberLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Text,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
            IsRequired: false,
            SectionKey: "basic-data",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "delivery_start",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeliveryStartLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.DateTime,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
            IsRequired: false,
            SectionKey: "basic-data",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "delivery_time",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeliveryTimeLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Number,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
            IsRequired: false,
            SectionKey: "basic-data",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "distance",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDistanceLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Number,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
            IsRequired: false,
            SectionKey: "basic-data",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "test_flight",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTestFlightLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Toggle,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
            IsRequired: false,
            SectionKey: "basic-data",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "technical_issue_type",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTechnicalIssueTypeLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "status",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldStatusLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "target_type",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTargetTypeLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "reason",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldReasonLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "comment",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldCommentLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Text,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "radio_frequency",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldRadioFrequencyLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "frequencies-parameters",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "video_frequency",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldVideoFrequencyLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "frequencies-parameters",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "warhead_type",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldWarheadTypeLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.ConfirmApplySelector,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ConfirmApply,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "detonator",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDetonatorLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "target-result",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText),
        new(
            FieldKey: "nsu",
            Label: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldNsuLabel,
            Kind: global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect,
            SelectorMode: global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ImmediatePick,
            IsRequired: false,
            SectionKey: "frequencies-parameters",
            PlaceholderText: global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText)
    ];

    public Task<global::App.Mobile.Android.Lookup.MobileLookupCatalogSnapshot> GetReportDraftFieldsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<string, IReadOnlyList<global::App.Mobile.Android.Lookup.MobileLookupOption>> optionsByFieldKey =
            FieldDefinitions.ToDictionary(
                field => field.FieldKey,
                field => GetStubOptionsForField(field).ToArray() as IReadOnlyList<global::App.Mobile.Android.Lookup.MobileLookupOption>,
                StringComparer.Ordinal);

        var snapshot = new global::App.Mobile.Android.Lookup.MobileLookupCatalogSnapshot(
            SnapshotId: Guid.NewGuid().ToString("N"),
            LoadedAtUtc: DateTimeOffset.UtcNow,
            Fields: FieldDefinitions,
            OptionsByFieldKey: optionsByFieldKey);

        return Task.FromResult(snapshot);
    }

    private static IEnumerable<global::App.Mobile.Android.Lookup.MobileLookupOption> GetStubOptionsForField(
        global::App.Mobile.Android.Lookup.MobileLookupFieldDefinition field)
    {
        if (field.Kind is not global::App.Mobile.Android.Lookup.MobileLookupFieldKind.SearchableSingleSelect
            and not global::App.Mobile.Android.Lookup.MobileLookupFieldKind.ConfirmApplySelector)
        {
            return Array.Empty<global::App.Mobile.Android.Lookup.MobileLookupOption>();
        }

        return
        [
            new global::App.Mobile.Android.Lookup.MobileLookupOption(
                OptionKey: $"{field.FieldKey}-stub-1",
                DisplayText: global::App.Mobile.Android.Localization.MobileUiText.GetLookupStubOptionText(1),
                SortOrder: 1,
                IsAvailable: true),
            new global::App.Mobile.Android.Lookup.MobileLookupOption(
                OptionKey: $"{field.FieldKey}-stub-2",
                DisplayText: global::App.Mobile.Android.Localization.MobileUiText.GetLookupStubOptionText(2),
                SortOrder: 2,
                IsAvailable: true)
        ];
    }
}