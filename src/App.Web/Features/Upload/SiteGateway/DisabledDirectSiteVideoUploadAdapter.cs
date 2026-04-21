namespace App.Web.Features.Upload.SiteGateway;

public sealed class DisabledDirectSiteVideoUploadAdapter : IDirectSiteVideoUploadAdapter
{
    private const string DisabledMessage =
        "Direct site upload adapter boundary is registered, but production site upload is intentionally not configured in this bounded step.";

    public Task<DirectSiteVideoUploadResult> UploadAsync(
        DirectSiteVideoUploadDraft draft,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new InvalidOperationException("Direct site upload content stream must be readable.");
        }

        return Task.FromResult(DirectSiteVideoUploadResult.NotConfigured(DisabledMessage));
    }
}