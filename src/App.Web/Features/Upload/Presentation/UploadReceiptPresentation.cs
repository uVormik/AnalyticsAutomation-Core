using BuildingBlocks.Contracts.VideoUpload;

namespace App.Web.Features.Upload.Presentation;

public sealed record UploadReceiptPresentationModel(
    string Status,
    string Title,
    string Message,
    string Tone,
    bool IsAccepted,
    bool IsUnsupported);

public static class UploadReceiptPresentation
{
    public static UploadReceiptPresentationModel FromStatus(string? status) => status switch
    {
        VideoUploadReceiptStatuses.Accepted => new UploadReceiptPresentationModel(
            VideoUploadReceiptStatuses.Accepted,
            "Receipt accepted",
            "The backend accepted the upload receipt.",
            "success",
            true,
            false),

        VideoUploadReceiptStatuses.AlreadyAccepted => new UploadReceiptPresentationModel(
            VideoUploadReceiptStatuses.AlreadyAccepted,
            "Receipt already accepted",
            "The backend has already accepted this idempotent upload receipt.",
            "info",
            true,
            false),

        VideoUploadReceiptStatuses.Rejected => new UploadReceiptPresentationModel(
            VideoUploadReceiptStatuses.Rejected,
            "Receipt rejected",
            "The backend rejected the upload receipt. The upload flow must stop and show the error.",
            "danger",
            false,
            false),

        _ => new UploadReceiptPresentationModel(
            string.IsNullOrWhiteSpace(status) ? "UNSUPPORTED" : status,
            "Unsupported receipt status",
            "The backend returned an upload receipt status that this web client does not support yet.",
            "danger",
            false,
            true)
    };
}