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

        throw new NotSupportedException(
            "S2-48 defines the secure storage boundary only; token persistence is deferred.");
    }

    public ValueTask ClearSessionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}