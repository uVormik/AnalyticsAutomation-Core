using App.Desktop.Boundaries;

namespace App.Desktop.Services.GroupTree;

public sealed class FakeDesktopGroupTreeClient : IDesktopGroupTreeClient
{
    public ValueTask<DesktopGroupTreeLoadResult> GetGroupTreeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(DesktopGroupTreeLoadResult.Loaded(DesktopGroupTreeNode.VisualSmokeNodes));
    }
}