using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class DesktopSignInViewModel(IDesktopSignInService signInService)
{
    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public Guid? DeviceId { get; set; }

    public bool IsBusy { get; private set; }

    public DesktopSignInResult LastResult { get; private set; } = DesktopSignInResult.NotStarted;

    public async ValueTask<DesktopSignInResult> SignInAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IsBusy = true;
        try
        {
            LastResult = await signInService.SignInAsync(
                Login,
                Password,
                DeviceId,
                cancellationToken);

            return LastResult;
        }
        finally
        {
            Password = string.Empty;
            IsBusy = false;
        }
    }
}