using App.Desktop.Boundaries;

namespace App.Desktop.Services.Placeholders;

public sealed class PlaceholderSecureSessionStorage : ISecureSessionStorage
{
    public ValueTask<SecureSessionDescriptor?> ReadSessionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult<SecureSessionDescriptor?>(null);
    }

    public ValueTask StoreSessionAsync(SecureSessionDescriptor session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        // TODO(S2-50 follow-up): replace only after an approved Windows secure token storage task.
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearSessionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}