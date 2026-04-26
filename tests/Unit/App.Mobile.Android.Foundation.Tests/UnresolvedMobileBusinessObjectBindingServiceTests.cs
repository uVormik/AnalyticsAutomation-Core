namespace App.Mobile.Android.Foundation.Tests;

public sealed class UnresolvedMobileBusinessObjectBindingServiceTests
{
    private readonly global::App.Mobile.Android.Services.Local.UnresolvedMobileBusinessObjectBindingService _service = new();

    [Fact]
    public async Task DefaultBindingStateIsUnresolved()
    {
        var snapshot = await _service.GetCurrentAsync();

        Assert.Equal(
            global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.Unresolved,
            snapshot.ApprovalState);
        Assert.Null(snapshot.LocalIntent);
    }

    [Fact]
    public async Task DefaultSnapshotHasNoApprovedBusinessObjectKey()
    {
        var snapshot = await _service.GetCurrentAsync();

        Assert.Null(snapshot.ApprovedBusinessObjectKey);
    }

    [Fact]
    public async Task LocalIntentCanBeCreatedWithoutBusinessObjectKey()
    {
        var snapshot = await _service.CreateOrUpdateLocalIntentAsync("Inspection draft", "local note");

        Assert.NotNull(snapshot.LocalIntent);
        Assert.Equal("Inspection draft", snapshot.LocalIntent.DisplayTitle);
        Assert.Equal("local note", snapshot.LocalIntent.NoteText);
        Assert.True(snapshot.LocalIntent.IsLocalOnly);
        Assert.Null(snapshot.ApprovedBusinessObjectKey);
    }

    [Fact]
    public async Task LocalIntentIdIsNotExposedAsBusinessObjectKey()
    {
        var snapshot = await _service.CreateOrUpdateLocalIntentAsync("Inspection draft", null);

        Assert.NotNull(snapshot.LocalIntent);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.LocalIntent.LocalIntentId));
        Assert.NotEqual(snapshot.LocalIntent.LocalIntentId, snapshot.ApprovedBusinessObjectKey);
        Assert.Null(snapshot.ApprovedBusinessObjectKey);
    }

    [Fact]
    public async Task ClearIntentReturnsUnresolvedWithoutApprovedKey()
    {
        await _service.CreateOrUpdateLocalIntentAsync("Inspection draft", "local note");

        var snapshot = await _service.ClearLocalIntentAsync();

        Assert.Equal(
            global::App.Mobile.Android.BusinessObjectBinding.MobileBusinessObjectBindingApprovalState.Unresolved,
            snapshot.ApprovalState);
        Assert.Null(snapshot.LocalIntent);
        Assert.Null(snapshot.ApprovedBusinessObjectKey);
    }
}