using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class DesktopSignInService(
    IDesktopAuthClient authClient,
    IDesktopSessionState sessionState) : IDesktopSignInService
{
    public async ValueTask<DesktopSignInResult> SignInAsync(
        string login,
        string password,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return DesktopSignInResult.Rejected("missing_credentials");
        }

        DesktopAuthResult authResult;
        try
        {
            authResult = await authClient.SignInAsync(
                new DesktopSignInRequest(login, password, deviceId),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return DesktopSignInResult.Unavailable("sign_in_unavailable");
        }

        if (!authResult.IsSuccess)
        {
            return ToSignInFailure(authResult);
        }

        if (authResult.Session is null)
        {
            return DesktopSignInResult.Failed("sign_in_invalid_session");
        }

        try
        {
            var snapshot = await sessionState.SetSignedInAsync(
                authResult.Session,
                cancellationToken);

            return DesktopSignInResult.Succeeded(snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return DesktopSignInResult.Failed("session_update_failed");
        }
    }

    private static DesktopSignInResult ToSignInFailure(DesktopAuthResult authResult)
    {
        string? code = authResult.Error?.Code;

        return authResult.Status switch
        {
            DesktopAuthStatus.Rejected => DesktopSignInResult.Rejected(code),
            DesktopAuthStatus.Failed => DesktopSignInResult.Failed(code),
            DesktopAuthStatus.Unavailable => DesktopSignInResult.Unavailable(code),
            _ => DesktopSignInResult.Failed("sign_in_failed")
        };
    }
}