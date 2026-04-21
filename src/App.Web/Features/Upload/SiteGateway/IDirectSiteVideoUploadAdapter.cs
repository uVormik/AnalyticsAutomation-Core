namespace App.Web.Features.Upload.SiteGateway;

public interface IDirectSiteVideoUploadAdapter
{
    Task<DirectSiteVideoUploadResult> UploadAsync(
        DirectSiteVideoUploadDraft draft,
        Stream content,
        CancellationToken cancellationToken = default);
}