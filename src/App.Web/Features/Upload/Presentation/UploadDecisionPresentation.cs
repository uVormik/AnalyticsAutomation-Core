using BuildingBlocks.Contracts.VideoUpload;

namespace App.Web.Features.Upload.Presentation;

public sealed record UploadDecisionPresentationModel(
    string Decision,
    string Title,
    string Message,
    string Tone,
    bool CanContinue,
    bool IsUnsupported);

public static class UploadDecisionPresentation
{
    public static UploadDecisionPresentationModel FromDecision(string? decision) => decision switch
    {
        PreUploadCheckDecisions.Allow => new UploadDecisionPresentationModel(
            PreUploadCheckDecisions.Allow,
            "Upload allowed",
            "The backend pre-upload check allows the direct site upload to continue.",
            "success",
            true,
            false),

        PreUploadCheckDecisions.BlockHardDuplicate => new UploadDecisionPresentationModel(
            PreUploadCheckDecisions.BlockHardDuplicate,
            "Hard duplicate blocked",
            "The backend detected an exact duplicate and blocked this upload.",
            "danger",
            false,
            false),

        PreUploadCheckDecisions.AllowWithReview => new UploadDecisionPresentationModel(
            PreUploadCheckDecisions.AllowWithReview,
            "Upload allowed with review",
            "The upload may continue, but the backend will route it for review.",
            "warning",
            true,
            false),

        PreUploadCheckDecisions.BlockPossibleFalsification => new UploadDecisionPresentationModel(
            PreUploadCheckDecisions.BlockPossibleFalsification,
            "Possible falsification blocked",
            "The backend blocked this upload because it matches a possible falsification rule.",
            "danger",
            false,
            false),

        _ => new UploadDecisionPresentationModel(
            string.IsNullOrWhiteSpace(decision) ? "UNSUPPORTED" : decision,
            "Unsupported upload decision",
            "The backend returned an upload decision that this web client does not support yet.",
            "danger",
            false,
            true)
    };
}