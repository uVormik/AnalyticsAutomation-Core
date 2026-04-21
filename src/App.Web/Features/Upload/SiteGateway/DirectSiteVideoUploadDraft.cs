namespace App.Web.Features.Upload.SiteGateway;

public sealed record DirectSiteVideoUploadDraft(
    Guid PreUploadCheckId,
    string FileName,
    long SizeBytes,
    string ByteSha256,
    string? ContentType);