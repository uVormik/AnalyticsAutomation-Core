namespace App.Mobile.Android.Foundation.Tests;

public sealed class LocalMobileReportDraftValidationServiceTests
{
    [Fact]
    public void ValidateForLocalQueue_NullDraft_ReturnsInvalidResult()
    {
        var service = new global::App.Mobile.Android.Services.Local.LocalMobileReportDraftValidationService();

        var result = service.ValidateForLocalQueue(null);

        Assert.False(result.IsValidForLocalQueue);
        Assert.Single(result.Issues);
        Assert.Contains("серверную проверку", result.SummaryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForLocalQueue_MissingRequiredFields_ReturnsInvalidResult()
    {
        var service = new global::App.Mobile.Android.Services.Local.LocalMobileReportDraftValidationService();

        var result = service.ValidateForLocalQueue(CreateDraft(fillRequiredFields: false, includeVideoAttachment: true));

        Assert.False(result.IsValidForLocalQueue);
        Assert.Contains(result.Issues, issue => issue.FieldKey == "device_type");
        Assert.Contains(result.Issues, issue => issue.FieldKey == "serial_number");
        Assert.Contains(result.Issues, issue => issue.FieldKey == "delivery_start");
        Assert.Contains(result.Issues, issue => issue.FieldKey == "delivery_time");
        Assert.Contains(result.Issues, issue => issue.FieldKey == "distance");
        Assert.Contains(result.Issues, issue => issue.FieldKey == "target_type");
        Assert.Contains(result.Issues, issue => issue.FieldKey == "reason");
    }

    [Fact]
    public void ValidateForLocalQueue_RequiredFieldsFilledButNoVideo_ReturnsInvalidResult()
    {
        var service = new global::App.Mobile.Android.Services.Local.LocalMobileReportDraftValidationService();

        var result = service.ValidateForLocalQueue(CreateDraft(fillRequiredFields: true, includeVideoAttachment: false));

        Assert.False(result.IsValidForLocalQueue);
        Assert.Contains(
            result.Issues,
            issue => issue.Message.Contains("видео", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateForLocalQueue_RequiredFieldsFilledAndVideoAttachmentPresent_ReturnsValidResult()
    {
        var service = new global::App.Mobile.Android.Services.Local.LocalMobileReportDraftValidationService();

        var result = service.ValidateForLocalQueue(CreateDraft(fillRequiredFields: true, includeVideoAttachment: true));

        Assert.True(result.IsValidForLocalQueue);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateForLocalQueue_OptionalFieldsMissing_DoNotBlockLocalQueue()
    {
        var service = new global::App.Mobile.Android.Services.Local.LocalMobileReportDraftValidationService();

        var draft = CreateDraft(fillRequiredFields: true, includeVideoAttachment: true);
        draft = draft with
        {
            Fields = draft.Fields.Select(field =>
            {
                if (field.IsRequired)
                {
                    return field;
                }

                return field with
                {
                    ValueText = string.Empty,
                    IsPlaceholder = true
                };
            }).ToArray()
        };

        var result = service.ValidateForLocalQueue(draft);

        Assert.True(result.IsValidForLocalQueue);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateForLocalQueue_SummaryIsLocalOnly_AndDoesNotMentionBackendSuccess()
    {
        var service = new global::App.Mobile.Android.Services.Local.LocalMobileReportDraftValidationService();

        var result = service.ValidateForLocalQueue(CreateDraft(fillRequiredFields: true, includeVideoAttachment: true));

        Assert.Contains("локаль", result.SummaryText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("серверную проверку", result.SummaryText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("успешно создан", result.SummaryText, StringComparison.OrdinalIgnoreCase);
    }

    private static global::App.Mobile.Android.Reports.MobileReportDraft CreateDraft(
        bool fillRequiredFields,
        bool includeVideoAttachment)
    {
        var now = new DateTimeOffset(2026, 4, 22, 8, 0, 0, TimeSpan.Zero);
        var fields = CreateFields(fillRequiredFields);
        var attachments = includeVideoAttachment
            ? new[]
            {
                new global::App.Mobile.Android.Reports.MobileReportAttachment(
                    AttachmentId: "attachment-1",
                    DraftId: "draft-1",
                    Kind: global::App.Mobile.Android.Reports.MobileReportAttachmentKind.Video,
                    FileName: "sample.mp4",
                    ContentType: "video/mp4",
                    SourceText: "Галерея",
                    AddedAtUtc: now,
                    SelectedMediaCacheKey: "cache-1",
                    HasLocalReadHandle: true)
            }
            : Array.Empty<global::App.Mobile.Android.Reports.MobileReportAttachment>();

        return new global::App.Mobile.Android.Reports.MobileReportDraft(
            DraftId: "draft-1",
            CreatedAtUtc: now,
            UpdatedAtUtc: now,
            Title: "FPV-отчет #1",
            Status: global::App.Mobile.Android.Reports.MobileReportDraftStatus.ReadyForAttachmentReview,
            Fields: fields,
            Attachments: attachments);
    }

    private static global::App.Mobile.Android.Reports.MobileReportDraftFieldValue[] CreateFields(bool fillRequiredFields)
    {
        return
        [
            CreateField("device_type", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeviceTypeLabel, true, fillRequiredFields),
            CreateField("serial_number", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldSerialNumberLabel, true, fillRequiredFields),
            CreateField("delivery_start", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeliveryStartLabel, true, fillRequiredFields),
            CreateField("delivery_time", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDeliveryTimeLabel, true, fillRequiredFields),
            CreateField("distance", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldDistanceLabel, true, fillRequiredFields),
            CreateField("target_type", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldTargetTypeLabel, true, fillRequiredFields),
            CreateField("reason", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldReasonLabel, true, fillRequiredFields),
            CreateField("comment", global::App.Mobile.Android.Localization.MobileUiText.ReportFieldCommentLabel, false, false)
        ];
    }

    private static global::App.Mobile.Android.Reports.MobileReportDraftFieldValue CreateField(
        string fieldKey,
        string label,
        bool isRequired,
        bool fillValue)
    {
        return new global::App.Mobile.Android.Reports.MobileReportDraftFieldValue(
            FieldKey: fieldKey,
            Label: label,
            ValueText: fillValue ? "Локальное значение" : global::App.Mobile.Android.Localization.MobileUiText.ReportFieldPlaceholderText,
            IsRequired: isRequired,
            IsPlaceholder: !fillValue)
        {
            SectionKey = "basic-data",
            FieldKind = global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Text,
            SelectorMode = global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.None
        };
    }
}