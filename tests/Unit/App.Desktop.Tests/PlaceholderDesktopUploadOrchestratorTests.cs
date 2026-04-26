using App.Desktop.Boundaries;
using App.Desktop.Services.Placeholders;

namespace App.Desktop.Tests;

public sealed class PlaceholderDesktopUploadOrchestratorTests
{
    [Fact]
    public async Task PrepareUploadAsyncDefersFullUploadFlow()
    {
        var orchestrator = new PlaceholderDesktopUploadOrchestrator();
        var draft = new DesktopUploadDraft(
            "business-object-1",
            new DesktopPickedFile(@"C:\video.mp4", SizeBytes: 42));

        var result = await orchestrator.PrepareUploadAsync(draft, CancellationToken.None);

        Assert.Equal(DesktopUploadOrchestrationStatus.Deferred, result.Status);
        Assert.False(result.ControlPlanePreCheckStarted);
        Assert.False(result.DirectSiteUploadStarted);
    }
}