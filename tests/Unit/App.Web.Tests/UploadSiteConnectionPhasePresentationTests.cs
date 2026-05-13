using App.Web.Features.Upload.Presentation;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadSiteConnectionPhasePresentationTests
{
    [Fact]
    public void SuccessIsTerminalWithCompleteProgress()
    {
        var model = UploadSiteConnectionPhasePresentation.FromPhase(UploadSiteConnectionPhase.Success);

        Assert.True(model.IsTerminal);
        Assert.Equal(100, model.ProgressPercent);
        Assert.Equal("success", model.Tone);
    }

    [Fact]
    public void FlowPhasesAdvanceBeforeSuccess()
    {
        Assert.True(UploadSiteConnectionPhasePresentation.FromPhase(UploadSiteConnectionPhase.HashingFile).ProgressPercent > 0);
        Assert.True(UploadSiteConnectionPhasePresentation.FromPhase(UploadSiteConnectionPhase.Precheck).ProgressPercent >
            UploadSiteConnectionPhasePresentation.FromPhase(UploadSiteConnectionPhase.HashingFile).ProgressPercent);
        Assert.True(UploadSiteConnectionPhasePresentation.FromPhase(UploadSiteConnectionPhase.ReceiptSubmit).ProgressPercent <
            UploadSiteConnectionPhasePresentation.FromPhase(UploadSiteConnectionPhase.Success).ProgressPercent);
    }
}