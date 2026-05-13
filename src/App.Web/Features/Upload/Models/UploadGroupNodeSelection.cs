using App.Web.Features.Upload.ControlPlane;

namespace App.Web.Features.Upload.Models;

public static class UploadGroupNodeSelection
{
    public static bool CanSelect(UploadControlPlaneGroupNode? node) =>
        node?.Id.HasValue == true && node.IsSelectable == true;
}