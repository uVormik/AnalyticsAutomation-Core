namespace App.Web.Features.Upload.ControlPlane;

public interface IUploadControlPlaneApi
{
    Task<UploadControlPlaneSignInResponse> SignInAsync(
        UploadControlPlaneSignInRequest request,
        CancellationToken cancellationToken = default);

    Task<UploadControlPlaneSignInResponse> RefreshAsync(
        UploadControlPlaneRefreshRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadControlPlaneGroupNode>> GetGroupTreeNodesAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}