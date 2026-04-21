using App.Web.Features.Upload.Presentation;

using BuildingBlocks.Contracts.VideoUpload;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadReceiptPresentationTests
{
    [Fact]
    public void Frozen_receipt_statuses_map_to_expected_acceptance_rules()
    {
        Assert.True(UploadReceiptPresentation.FromStatus(VideoUploadReceiptStatuses.Accepted).IsAccepted);
        Assert.True(UploadReceiptPresentation.FromStatus(VideoUploadReceiptStatuses.AlreadyAccepted).IsAccepted);
        Assert.False(UploadReceiptPresentation.FromStatus(VideoUploadReceiptStatuses.Rejected).IsAccepted);
    }

    [Fact]
    public void Unsupported_receipt_status_is_not_mapped_to_a_fake_state()
    {
        var model = UploadReceiptPresentation.FromStatus("NEW_RECEIPT_STATUS");

        Assert.True(model.IsUnsupported);
        Assert.False(model.IsAccepted);
        Assert.Equal("NEW_RECEIPT_STATUS", model.Status);
    }
}