namespace App.Web.Features.Upload.Presentation;

public enum UploadSiteConnectionPhase
{
    Idle,
    SigningIn,
    LoadingGroups,
    HashingFile,
    Precheck,
    LocalSiteUpload,
    ReceiptSubmit,
    Success,
    Blocked,
    Failed
}

public sealed record UploadSiteConnectionPhaseModel(
    UploadSiteConnectionPhase Phase,
    string Label,
    string Tone,
    int ProgressPercent,
    bool IsTerminal);

public static class UploadSiteConnectionPhasePresentation
{
    public static UploadSiteConnectionPhaseModel FromPhase(UploadSiteConnectionPhase phase) => phase switch
    {
        UploadSiteConnectionPhase.SigningIn => new(phase, "Signing in", "info", 10, false),
        UploadSiteConnectionPhase.LoadingGroups => new(phase, "Loading groups", "info", 20, false),
        UploadSiteConnectionPhase.HashingFile => new(phase, "Hashing file", "info", 35, false),
        UploadSiteConnectionPhase.Precheck => new(phase, "Pre-upload check", "info", 55, false),
        UploadSiteConnectionPhase.LocalSiteUpload => new(phase, "Local site upload", "info", 75, false),
        UploadSiteConnectionPhase.ReceiptSubmit => new(phase, "Receipt confirmation", "info", 90, false),
        UploadSiteConnectionPhase.Success => new(phase, "Upload confirmed", "success", 100, true),
        UploadSiteConnectionPhase.Blocked => new(phase, "Upload blocked", "warning", 100, true),
        UploadSiteConnectionPhase.Failed => new(phase, "Upload failed", "danger", 100, true),
        _ => new(UploadSiteConnectionPhase.Idle, "Idle", "secondary", 0, false)
    };
}