namespace App.Desktop.Boundaries;

public interface ISecureSessionStorage
{
    ValueTask<SecureSessionDescriptor?> ReadSessionAsync(CancellationToken cancellationToken);

    ValueTask StoreSessionAsync(SecureSessionDescriptor session, CancellationToken cancellationToken);

    ValueTask ClearSessionAsync(CancellationToken cancellationToken);
}

public sealed record SecureSessionDescriptor(
    string SessionKey,
    DateTimeOffset? ExpiresAtUtc);