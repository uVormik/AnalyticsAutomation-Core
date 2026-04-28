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

    private static DesktopAuthenticatedSession CreateAuthenticatedSession()
    {
        return DesktopAuthenticatedSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Desktop Operator",
            CreateSensitiveValue("access"),
            CreateSensitiveValue("refresh"),
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
}