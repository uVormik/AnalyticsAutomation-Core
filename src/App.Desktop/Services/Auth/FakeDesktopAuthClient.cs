using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class FakeDesktopAuthClient : IDesktopAuthClient
{
    public const string VisualSmokeLogin = "visual-smoke-user";
    public const string VisualSmokePassword = "visual-smoke-password";

    private static readonly Guid SessionId = Guid.Parse("9b64d538-751c-4d41-8423-2a66d60a475d");
    private static readonly Guid UserId = Guid.Parse("1c9f60aa-f495-4ce0-b2e2-e00cc9d7fd1e");

    public ValueTask<DesktopAuthResult> SignInAsync(
        DesktopSignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Login, VisualSmokeLogin, StringComparison.Ordinal)
            || !string.Equals(request.Password, VisualSmokePassword, StringComparison.Ordinal))
        {
            return ValueTask.FromResult(DesktopAuthResult.Rejected(
                "fake_auth_invalid_credentials",
                "Dev/test fake auth rejected the supplied credentials."));
        }

        DateTimeOffset issuedAtUtc = DateTimeOffset.UtcNow;
        var session = DesktopAuthenticatedSession.Create(
            SessionId,
            UserId,
            request.DeviceId,
            "Visual Smoke User",
            "dev-fake-auth-access",
            refreshToken: null,
            issuedAtUtc,
            issuedAtUtc.AddMinutes(15),
            isOfflineRestricted: true);

        return ValueTask.FromResult(DesktopAuthResult.Succeeded(session));
    }
}