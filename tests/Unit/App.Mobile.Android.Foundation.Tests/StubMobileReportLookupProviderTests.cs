namespace App.Mobile.Android.Foundation.Tests;

public sealed class StubMobileReportLookupProviderTests
{
    [Fact]
    public async Task ProviderReturnsFieldDefinitions()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();

        Assert.NotEmpty(snapshot.Fields);
    }

    [Fact]
    public async Task ProviderIncludesExpectedLocalFieldKeys()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();
        var fieldKeys = snapshot.Fields.Select(field => field.FieldKey).ToArray();

        Assert.Contains("device_type", fieldKeys);
        Assert.Contains("serial_number", fieldKeys);
        Assert.Contains("delivery_start", fieldKeys);
        Assert.Contains("delivery_time", fieldKeys);
        Assert.Contains("distance", fieldKeys);
        Assert.Contains("target_type", fieldKeys);
        Assert.Contains("reason", fieldKeys);
        Assert.Contains("comment", fieldKeys);
        Assert.Contains("radio_frequency", fieldKeys);
        Assert.Contains("video_frequency", fieldKeys);
        Assert.Contains("test_flight", fieldKeys);
        Assert.Contains("technical_issue_type", fieldKeys);
        Assert.Contains("status", fieldKeys);
        Assert.Contains("warhead_type", fieldKeys);
        Assert.Contains("detonator", fieldKeys);
        Assert.Contains("nsu", fieldKeys);
    }

    [Fact]
    public async Task SelectorFieldsHaveStubOptionsMarkedAsNonFinal()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();

        var selectorOptions = snapshot.OptionsByFieldKey["device_type"];

        Assert.NotEmpty(selectorOptions);
        Assert.All(selectorOptions, option => Assert.Contains("Заглушка —", option.DisplayText, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProviderDoesNotReturnRealFinalDictionaries()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();

        Assert.NotNull(snapshot.OptionsByFieldKey);
        Assert.All(snapshot.OptionsByFieldKey.Values, options =>
            Assert.All(options, option => Assert.Contains("Заглушка —", option.DisplayText, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task WarheadTypeUsesConfirmApplySelector()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();
        var field = Assert.Single(snapshot.Fields.Where(item => item.FieldKey == "warhead_type"));

        Assert.Equal(global::App.Mobile.Android.Lookup.MobileLookupFieldKind.ConfirmApplySelector, field.Kind);
        Assert.Equal(global::App.Mobile.Android.Lookup.MobileLookupSelectorMode.ConfirmApply, field.SelectorMode);
    }

    [Fact]
    public async Task TestFlightIsToggle()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();
        var field = Assert.Single(snapshot.Fields.Where(item => item.FieldKey == "test_flight"));

        Assert.Equal(global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Toggle, field.Kind);
    }

    [Fact]
    public async Task TextNumberDateFieldsHaveExpectedKinds()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();

        Assert.Equal(
            global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Text,
            Assert.Single(snapshot.Fields.Where(item => item.FieldKey == "serial_number")).Kind);
        Assert.Equal(
            global::App.Mobile.Android.Lookup.MobileLookupFieldKind.Number,
            Assert.Single(snapshot.Fields.Where(item => item.FieldKey == "delivery_time")).Kind);
        Assert.Equal(
            global::App.Mobile.Android.Lookup.MobileLookupFieldKind.DateTime,
            Assert.Single(snapshot.Fields.Where(item => item.FieldKey == "delivery_start")).Kind);
    }
}