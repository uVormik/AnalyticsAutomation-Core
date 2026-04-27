using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class UnavailableDesktopAuthClient : IDesktopAuthClient
{
    public ValueTask<DesktopAuthResult> SignInAsync(
        DesktopSignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(DesktopAuthResult.Unavailable(
            "desktop_auth_not_configured",
            "S2-50 registers the desktop auth boundary; control-plane sign-in endpoint configuration is deferred."));
    }
}