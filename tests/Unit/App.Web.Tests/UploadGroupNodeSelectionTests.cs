using App.Web.Features.Upload.ControlPlane;
using App.Web.Features.Upload.Models;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadGroupNodeSelectionTests
{
    [Fact]
    public void CanSelectRequiresIdAndSelectableFlag()
    {
        var selectableNode = new UploadControlPlaneGroupNode
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            IsSelectable = true
        };

        Assert.True(UploadGroupNodeSelection.CanSelect(selectableNode));
        Assert.False(UploadGroupNodeSelection.CanSelect(new UploadControlPlaneGroupNode { IsSelectable = true }));
        Assert.False(UploadGroupNodeSelection.CanSelect(new UploadControlPlaneGroupNode
        {
            Id = selectableNode.Id,
            IsSelectable = false
        }));
    }
}