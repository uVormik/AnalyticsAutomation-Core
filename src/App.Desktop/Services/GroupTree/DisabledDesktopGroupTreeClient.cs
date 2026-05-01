using App.Desktop.Boundaries;

namespace App.Desktop.Services.GroupTree;

public sealed class DisabledDesktopGroupTreeClient : IDesktopGroupTreeClient
{
    public ValueTask<DesktopGroupTreeLoadResult> GetGroupTreeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(DesktopGroupTreeLoadResult.Disabled);
    }
}