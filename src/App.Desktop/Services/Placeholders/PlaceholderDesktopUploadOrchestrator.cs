using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderDesktopUploadOrchestrator : IDesktopUploadOrchestrator
{
    public ValueTask<DesktopUploadOrchestrationResult> PrepareUploadAsync(
        DesktopUploadDraft draft,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new DesktopUploadOrchestrationResult(
            DesktopUploadOrchestrationStatus.Deferred,
            "S2-48 skeleton only; full upload orchestration is deferred.",
            ControlPlanePreCheckStarted: false,
            DirectSiteUploadStarted: false));
    }
}