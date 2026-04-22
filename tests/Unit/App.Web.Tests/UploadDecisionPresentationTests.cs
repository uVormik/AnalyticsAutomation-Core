using App.Web.Features.Upload.Presentation;

using BuildingBlocks.Contracts.VideoUpload;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadDecisionPresentationTests
{
    [Fact]
    public void FrozenPreUploadDecisionsMapToExpectedContinueRules()
    {
        Assert.True(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.Allow).CanContinue);
        Assert.True(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.AllowWithReview).CanContinue);
        Assert.False(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.BlockHardDuplicate).CanContinue);
        Assert.False(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.BlockPossibleFalsification).CanContinue);
    }

    [Fact]
    public void UnsupportedPreUploadDecisionIsNotMappedToAFakeState()
    {
        var model = UploadDecisionPresentation.FromDecision("NEW_BACKEND_DECISION");

        Assert.True(model.IsUnsupported);
        Assert.False(model.CanContinue);
        Assert.Equal("NEW_BACKEND_DECISION", model.Decision);
    }
}