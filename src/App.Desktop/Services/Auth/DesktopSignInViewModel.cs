using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class DesktopSignInViewModel(
    IDesktopSignInService signInService,
    IDesktopSessionState sessionState)
{
    private int _isBusy;

    public DesktopSignInViewModel(IDesktopSignInService signInService)
        : this(signInService, new DesktopSessionState(new DisabledDesktopSessionStore()))
    {
    }

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public Guid? DeviceId { get; set; }

    public bool IsBusy => Volatile.Read(ref _isBusy) == 1;

    public bool CanSubmit =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Login)
        && !string.IsNullOrWhiteSpace(Password);

    public bool IsSignedIn => CurrentSession.IsSignedIn;

    public DesktopSessionSnapshot CurrentSession { get; private set; } = sessionState.Current;

    public string SignedInUserContextMessage =>
        DesktopSignedInShellText.CreateUserContextMessage(CurrentSession);

    public DesktopSignInResult LastResult { get; private set; } = DesktopSignInResult.NotStarted;

    public void SetLoginInput(string? login)
    {
        Login = login ?? string.Empty;
    }

    public void SetPasswordInput(string? password)
    {
        Password = password ?? string.Empty;
    }

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

            if (LastResult.IsSuccess && LastResult.Session is not null)
            {
                CurrentSession = LastResult.Session;
            }

            return LastResult;
        }
        finally
        {
            Password = string.Empty;
            Volatile.Write(ref _isBusy, 0);
        }
    }

    public async ValueTask<DesktopSessionSnapshot> SignOutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.Exchange(ref _isBusy, 1) == 1)
        {
            LastResult = DesktopSignInResult.InProgress;
            return CurrentSession;
        }

        try
        {
            Password = string.Empty;

            CurrentSession = await sessionState.SignOutAsync(cancellationToken);
            LastResult = DesktopSignInResult.SignedOut;

            return CurrentSession;
        }
        finally
        {
            Password = string.Empty;
            Volatile.Write(ref _isBusy, 0);
        }
    }
}