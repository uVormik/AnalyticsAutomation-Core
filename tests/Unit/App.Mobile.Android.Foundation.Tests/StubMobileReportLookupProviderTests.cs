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
    }

    [Fact]
    public async Task ProviderDoesNotReturnFinalOptionDictionariesForLookupFields()
    {
        var provider = new global::App.Mobile.Android.Services.Stubs.StubMobileReportLookupProvider();

        var snapshot = await provider.GetReportDraftFieldsAsync();

        Assert.NotNull(snapshot.OptionsByFieldKey);
        Assert.All(snapshot.OptionsByFieldKey.Values, options => Assert.Empty(options));
    }
}