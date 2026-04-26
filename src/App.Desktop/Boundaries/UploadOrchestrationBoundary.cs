namespace App.Desktop.Boundaries;

public interface IDesktopUploadOrchestrator
{
    ValueTask<DesktopUploadOrchestrationResult> PrepareUploadAsync(
        DesktopUploadDraft draft,
        CancellationToken cancellationToken);
}

public sealed record DesktopUploadDraft(
    string BusinessObjectKey,
    DesktopPickedFile File);

public sealed record DesktopUploadOrchestrationResult(
    DesktopUploadOrchestrationStatus Status,
    string Reason,
    bool ControlPlanePreCheckStarted,
    bool DirectSiteUploadStarted);

public enum DesktopUploadOrchestrationStatus
{
    Deferred
}