namespace App.Web.Features.Upload.ControlPlane;

public sealed class InMemoryUploadControlPlaneSessionStore : IUploadControlPlaneSessionStore
{
    private UploadControlPlaneSession? _session;

    public ValueTask SetAsync(
        UploadControlPlaneSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        _session = session;

        return ValueTask.CompletedTask;
    }

    public ValueTask<UploadControlPlaneSession?> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_session);
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _session = null;

        return ValueTask.CompletedTask;
    }
}