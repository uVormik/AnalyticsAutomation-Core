namespace App.Desktop.Services.Upload;

public interface IDesktopDirectSiteProviderClient
{
    ValueTask<DesktopDirectSiteUploadResponse> UploadAsync(
        DesktopDirectSiteUploadRequest request,
        CancellationToken cancellationToken);
}