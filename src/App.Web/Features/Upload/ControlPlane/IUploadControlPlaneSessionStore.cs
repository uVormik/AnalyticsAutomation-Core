namespace App.Web.Features.Upload.ControlPlane;

public interface IUploadControlPlaneSessionStore
{
    ValueTask SetAsync(
        UploadControlPlaneSession session,
        CancellationToken cancellationToken = default);

    ValueTask<UploadControlPlaneSession?> GetAsync(CancellationToken cancellationToken = default);

    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}