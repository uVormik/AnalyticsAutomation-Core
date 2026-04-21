using App.Web.Features.Upload.Presentation;
using BuildingBlocks.Contracts.VideoUpload;
using Xunit;

namespace App.Web.Tests;

public sealed class UploadDecisionPresentationTests
{
    [Fact]
    public void Frozen_pre_upload_decisions_map_to_expected_continue_rules()
    {
        Assert.True(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.Allow).CanContinue);
        Assert.True(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.AllowWithReview).CanContinue);
        Assert.False(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.BlockHardDuplicate).CanContinue);
        Assert.False(UploadDecisionPresentation.FromDecision(PreUploadCheckDecisions.BlockPossibleFalsification).CanContinue);
    }

    [Fact]
    public void Unsupported_pre_upload_decision_is_not_mapped_to_a_fake_state()
    {
        var model = UploadDecisionPresentation.FromDecision("NEW_BACKEND_DECISION");

        Assert.True(model.IsUnsupported);
        Assert.False(model.CanContinue);
        Assert.Equal("NEW_BACKEND_DECISION", model.Decision);
    }
}