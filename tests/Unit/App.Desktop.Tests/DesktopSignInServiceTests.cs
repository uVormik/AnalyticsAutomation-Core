using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;

namespace App.Desktop.Tests;

public sealed class DesktopSignInServiceTests
{
    [Fact]
    public async Task SuccessfulSignInCallsAuthClientAndUpdatesSessionState()
    {
        var session = CreateAuthenticatedSession();
        var authClient = new RecordingAuthClient(DesktopAuthResult.Succeeded(session));
        var sessionState = new DesktopSessionState(new DisabledDesktopSessionStore());
        var service = new DesktopSignInService(authClient, sessionState);

        var result = await service.SignInAsync(
            " desktop-operator ",
            CreateSensitiveValue("password"),
            session.DeviceId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DesktopSignInStatus.Succeeded, result.Status);
        Assert.True(result.Session!.IsSignedIn);
        Assert.Equal(session.UserId, sessionState.Current.UserId);
        Assert.Equal(session.DisplayName, sessionState.Current.DisplayName);
        Assert.Equal(1, authClient.CallCount);
        Assert.Equal("desktop-operator", authClient.LastRequest!.Login);
        Assert.DoesNotContain(session.AccessToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(session.RefreshToken!, result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewModelSuccessfulSignInSetsVisibleSignedInMessage()
    {
        var session = CreateAuthenticatedSession(displayName: "Desktop Operator");
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new RecordingAuthClient(DesktopAuthResult.Succeeded(session)),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = CreateSensitiveValue("password")
        };

        var result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Succeeded, result.Status);
        Assert.Contains("Signed in", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Desktop Operator", result.Message, StringComparison.Ordinal);
        Assert.Equal(result, viewModel.LastResult);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ViewModelRejectedCredentialsSetVisibleSafeErrorMessage()
    {
        var password = CreateSensitiveValue("password");
        var accessToken = CreateSensitiveValue("access");
        var rawServerMessage = $"invalid {password} {accessToken} Authorization: Bearer secret";
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new RecordingAuthClient(DesktopAuthResult.Rejected("invalid_credentials", rawServerMessage)),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = password
        };

        var result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Rejected, result.Status);
        Assert.Contains("not accepted", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(password, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(result, viewModel.LastResult);
    }

    [Fact]
    public async Task ViewModelUnavailableResultSetsVisibleSafeUnavailableMessage()
    {
        var password = CreateSensitiveValue("password");
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new ThrowingAuthClient(new HttpRequestException($"transport failed {password} Authorization: Bearer secret")),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = password
        };

        var result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Unavailable, result.Status);
        Assert.Contains("unavailable", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(password, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(result, viewModel.LastResult);
    }

    [Fact]
    public async Task ViewModelVisibleResultTextDoesNotExposePasswordTokensAuthorizationOrTokenLikeValues()
    {
        var password = CreateSensitiveValue("password");
        var accessToken = CreateSensitiveValue("access");
        var refreshToken = CreateSensitiveValue("refresh");
        const string authorizationHeader = "Authorization: Bearer secret-token";
        var session = CreateAuthenticatedSession(
            accessToken: accessToken,
            refreshToken: refreshToken,
            displayName: $"{authorizationHeader} accessToken refreshToken password");
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new RecordingAuthClient(DesktopAuthResult.Succeeded(session)),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = password
        };

        var result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Succeeded, result.Status);
        Assert.Equal("Signed in.", result.Message);
        Assert.DoesNotContain(password, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(authorizationHeader, result.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(FailedAuthResults))]
    public async Task FailedSignInDoesNotMutateExistingSignedInSession(DesktopAuthResult authResult)
    {
        var existingSession = CreateAuthenticatedSession();
        var authClient = new RecordingAuthClient(authResult);
        var sessionState = new DesktopSessionState(new DisabledDesktopSessionStore());
        await sessionState.SetSignedInAsync(existingSession, CancellationToken.None);
        var originalSnapshot = sessionState.Current;
        var service = new DesktopSignInService(authClient, sessionState);

        var result = await service.SignInAsync(
            "desktop-operator",
            CreateSensitiveValue("password"),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Same(originalSnapshot, sessionState.Current);
        Assert.Equal(existingSession.UserId, sessionState.Current.UserId);
        Assert.Equal(existingSession.DisplayName, sessionState.Current.DisplayName);
    }

    [Fact]
    public async Task ErrorResultTextDoesNotExposeCredentialsTokensAuthorizationOrRawMessages()
    {
        var login = $"desktop-{Guid.NewGuid():N}";
        var password = CreateSensitiveValue("password");
        var accessToken = CreateSensitiveValue("access");
        var refreshToken = CreateSensitiveValue("refresh");
        const string authorizationHeader = "Authorization: Bearer secret-token";
        const string rawBody = "{\"accessToken\":\"secret-token\"}";
        const string rawException = "HttpRequestException secret detail";
        string unsafeText = string.Join(
            ' ',
            login,
            password,
            accessToken,
            refreshToken,
            authorizationHeader,
            rawBody,
            rawException);
        var authClient = new RecordingAuthClient(DesktopAuthResult.Failed(unsafeText, unsafeText));
        var service = new DesktopSignInService(
            authClient,
            new DesktopSessionState(new DisabledDesktopSessionStore()));

        var result = await service.SignInAsync(
            login,
            password,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Failed, result.Status);
        Assert.DoesNotContain(login, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(authorizationHeader, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(rawBody, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(rawException, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(unsafeText, result.Error!.Code, StringComparison.Ordinal);
        Assert.DoesNotContain(unsafeText, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewModelClearsPasswordAfterAttempt()
    {
        var password = CreateSensitiveValue("password");
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new RecordingAuthClient(DesktopAuthResult.Rejected("invalid_credentials", "Rejected.")),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = password
        };

        _ = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(string.Empty, viewModel.Password);
        Assert.DoesNotContain(password, viewModel.LastResult.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModelCanSubmitRequiresLoginPasswordAndIdleState()
    {
        var viewModel = new DesktopSignInViewModel(new RecordingSignInService(DesktopSignInResult.InProgress));

        Assert.False(viewModel.CanSubmit);

        viewModel.Login = "desktop-operator";

        Assert.False(viewModel.CanSubmit);

        viewModel.Password = CreateSensitiveValue("password");

        Assert.True(viewModel.CanSubmit);
    }

    [Fact]
    public async Task ViewModelConcurrentDoubleSubmitIsIgnoredSafely()
    {
        var completion = new TaskCompletionSource<DesktopSignInResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var signInService = new BlockingSignInService(completion.Task);
        var password = CreateSensitiveValue("password");
        var viewModel = new DesktopSignInViewModel(signInService)
        {
            Login = "desktop-operator",
            Password = password
        };

        Task<DesktopSignInResult> firstAttempt = viewModel.SignInAsync(CancellationToken.None).AsTask();
        await signInService.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CanSubmit);
        Assert.Equal(DesktopSignInStatus.InProgress, viewModel.LastResult.Status);

        DesktopSignInResult secondAttempt = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.InProgress, secondAttempt.Status);
        Assert.Equal(1, signInService.CallCount);

        completion.SetResult(DesktopSignInResult.Succeeded(
            DesktopSessionSnapshot.FromSession(CreateAuthenticatedSession())));

        DesktopSignInResult finalResult = await firstAttempt;

        Assert.Equal(DesktopSignInStatus.Succeeded, finalResult.Status);
        Assert.Equal(string.Empty, viewModel.Password);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task MissingCredentialsStayRejectedWithoutCallingAuthClient()
    {
        var authClient = new RecordingAuthClient(
            DesktopAuthResult.Succeeded(CreateAuthenticatedSession()));
        var service = new DesktopSignInService(
            authClient,
            new DesktopSessionState(new DisabledDesktopSessionStore()));

        var result = await service.SignInAsync(
            "desktop-operator",
            "",
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Rejected, result.Status);
        Assert.Equal(0, authClient.CallCount);
    }

    public static IEnumerable<object[]> FailedAuthResults()
    {
        yield return [DesktopAuthResult.Rejected("invalid_credentials", "Rejected.")];
        yield return [DesktopAuthResult.Failed("sign_in_failed", "Failed.")];
        yield return [DesktopAuthResult.Unavailable("sign_in_unavailable", "Unavailable.")];
    }

    private static DesktopAuthenticatedSession CreateAuthenticatedSession(
        string? accessToken = null,
        string? refreshToken = null,
        string? displayName = "Desktop Operator")
    {
        return DesktopAuthenticatedSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            displayName,
            accessToken ?? CreateSensitiveValue("access"),
            refreshToken ?? CreateSensitiveValue("refresh"),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15),
            isOfflineRestricted: false);
    }

    private static string CreateSensitiveValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private sealed class RecordingAuthClient(DesktopAuthResult result) : IDesktopAuthClient
    {
        public int CallCount { get; private set; }

        public DesktopSignInRequest? LastRequest { get; private set; }

        public ValueTask<DesktopAuthResult> SignInAsync(
            DesktopSignInRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;
            LastRequest = request;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowingAuthClient(Exception exception) : IDesktopAuthClient
    {
        public ValueTask<DesktopAuthResult> SignInAsync(
            DesktopSignInRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            throw exception;
        }
    }

    private sealed class RecordingSignInService(DesktopSignInResult result) : IDesktopSignInService
    {
        public ValueTask<DesktopSignInResult> SignInAsync(
            string login,
            string password,
            Guid? deviceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(result);
        }
    }

    private sealed class BlockingSignInService(Task<DesktopSignInResult> resultTask) : IDesktopSignInService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public ValueTask<DesktopSignInResult> SignInAsync(
            string login,
            string password,
            Guid? deviceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;
            Started.TrySetResult();
            return new ValueTask<DesktopSignInResult>(resultTask);
        }
    }
}