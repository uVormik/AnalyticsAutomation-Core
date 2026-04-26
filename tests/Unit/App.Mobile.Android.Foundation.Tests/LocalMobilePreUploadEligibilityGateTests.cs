namespace App.Mobile.Android.Foundation.Tests;

public sealed class LocalMobilePreUploadEligibilityGateTests
{
    [Fact]
    public async Task EligibilityGateBlocksPreUploadCheckWhenUnresolved()
    {
        var bindingService = new global::App.Mobile.Android.Services.Local.UnresolvedMobileBusinessObjectBindingService();
        var gate = new global::App.Mobile.Android.Services.Local.LocalMobilePreUploadEligibilityGate(bindingService);

        var result = await gate.EvaluateAsync();

        Assert.False(result.CanRunProductionPreUploadCheck);
        Assert.Null(result.BindingSnapshot.ApprovedBusinessObjectKey);
    }

    [Fact]
    public async Task EligibilityGateMessageMentionsApprovedSourceIsNotDocumented()
    {
        var bindingService = new global::App.Mobile.Android.Services.Local.UnresolvedMobileBusinessObjectBindingService();
        var gate = new global::App.Mobile.Android.Services.Local.LocalMobilePreUploadEligibilityGate(bindingService);

        var result = await gate.EvaluateAsync();

        Assert.Contains("approved businessObjectKey source is not documented", result.MessageText);
        Assert.Contains("businessObjectKey", result.RequiredActionText);
    }

    [Fact]
    public async Task EligibilityGateDoesNotProduceFakeBusinessObjectKeyAfterLocalIntent()
    {
        var bindingService = new global::App.Mobile.Android.Services.Local.UnresolvedMobileBusinessObjectBindingService();
        await bindingService.CreateOrUpdateLocalIntentAsync("Inspection draft", "local note");
        var gate = new global::App.Mobile.Android.Services.Local.LocalMobilePreUploadEligibilityGate(bindingService);

        var result = await gate.EvaluateAsync();

        Assert.False(result.CanRunProductionPreUploadCheck);
        Assert.NotNull(result.BindingSnapshot.LocalIntent);
        Assert.Null(result.BindingSnapshot.ApprovedBusinessObjectKey);
    }
}