using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class DesktopSignInViewModel(IDesktopSignInService signInService)
{
    private int _isBusy;

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public Guid? DeviceId { get; set; }

    public bool IsBusy => Volatile.Read(ref _isBusy) == 1;

    public bool CanSubmit =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Login)
        && !string.IsNullOrWhiteSpace(Password);

    public DesktopSignInResult LastResult { get; private set; } = DesktopSignInResult.NotStarted;

    public async ValueTask<DesktopSignInResult> SignInAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.Exchange(ref _isBusy, 1) == 1)
        {
            LastResult = DesktopSignInResult.InProgress;
            return LastResult;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                LastResult = DesktopSignInResult.Rejected("missing_credentials");
                return LastResult;
            }

            LastResult = DesktopSignInResult.InProgress;

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
            Volatile.Write(ref _isBusy, 0);
        }
    }
}