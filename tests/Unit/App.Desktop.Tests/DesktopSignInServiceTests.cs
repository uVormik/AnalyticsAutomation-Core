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
        Assert.Contains("Вход выполнен", result.Message, StringComparison.Ordinal);
        Assert.Contains("Desktop Operator", result.Message, StringComparison.Ordinal);
        Assert.Equal(result, viewModel.LastResult);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ViewModelSuccessfulSignInTransitionsToSignedInShellState()
    {
        var session = CreateAuthenticatedSession(displayName: "Desktop Operator");
        var sessionState = new DesktopSessionState(new DisabledDesktopSessionStore());
        var viewModel = new DesktopSignInViewModel(
            new DesktopSignInService(
                new RecordingAuthClient(DesktopAuthResult.Succeeded(session)),
                sessionState),
            sessionState)
        {
            Login = "desktop-operator",
            Password = CreateSensitiveValue("password")
        };

        var result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Succeeded, result.Status);
        Assert.True(viewModel.IsSignedIn);
        Assert.Equal(DesktopSessionStatus.SignedIn, viewModel.CurrentSession.Status);
        Assert.Equal(session.UserId, viewModel.CurrentSession.UserId);
        Assert.Equal("Вход выполнен: Desktop Operator.", viewModel.SignedInUserContextMessage);
    }

    [Fact]
    public async Task ViewModelSignedInUserContextShowsSafeDisplayNameOnly()
    {
        var password = CreateSensitiveValue("password");
        var accessToken = CreateSensitiveValue("access");
        var refreshToken = CreateSensitiveValue("refresh");
        var session = CreateAuthenticatedSession(
            accessToken: accessToken,
            refreshToken: refreshToken,
            displayName: "Desktop Operator");
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new RecordingAuthClient(DesktopAuthResult.Succeeded(session)),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = password
        };

        _ = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal("Вход выполнен: Desktop Operator.", viewModel.SignedInUserContextMessage);
        Assert.DoesNotContain(password, viewModel.SignedInUserContextMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, viewModel.SignedInUserContextMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, viewModel.SignedInUserContextMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", viewModel.SignedInUserContextMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Authorization: Bearer secret")]
    [InlineData("accessToken")]
    [InlineData("refresh_token")]
    [InlineData("raw session_id")]
    [InlineData("password")]
    public async Task ViewModelSignedInUserContextHidesUnsafeDisplayName(string unsafeDisplayName)
    {
        var viewModel = new DesktopSignInViewModel(new DesktopSignInService(
            new RecordingAuthClient(DesktopAuthResult.Succeeded(
                CreateAuthenticatedSession(displayName: unsafeDisplayName))),
            new DesktopSessionState(new DisabledDesktopSessionStore())))
        {
            Login = "desktop-operator",
            Password = CreateSensitiveValue("password")
        };

        _ = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal("Вход выполнен.", viewModel.SignedInUserContextMessage);
        Assert.DoesNotContain(unsafeDisplayName, viewModel.SignedInUserContextMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewModelSignOutReturnsToSignInStateAndClearsSessionVisibleState()
    {
        var session = CreateAuthenticatedSession(displayName: "Desktop Operator");
        var sessionState = new DesktopSessionState(new DisabledDesktopSessionStore());
        var viewModel = new DesktopSignInViewModel(
            new DesktopSignInService(
                new RecordingAuthClient(DesktopAuthResult.Succeeded(session)),
                sessionState),
            sessionState)
        {
            Login = "desktop-operator",
            Password = CreateSensitiveValue("password")
        };

        _ = await viewModel.SignInAsync(CancellationToken.None);
        viewModel.Password = CreateSensitiveValue("password-after-signin");

        DesktopSessionSnapshot signedOut = await viewModel.SignOutAsync(CancellationToken.None);

        Assert.Equal(DesktopSessionStatus.SignedOut, signedOut.Status);
        Assert.False(viewModel.IsSignedIn);
        Assert.Equal(DesktopSessionStatus.SignedOut, viewModel.CurrentSession.Status);
        Assert.Null(viewModel.CurrentSession.UserId);
        Assert.Null(viewModel.CurrentSession.DisplayName);
        Assert.False(viewModel.CurrentSession.HasAccessToken);
        Assert.False(viewModel.CurrentSession.HasRefreshToken);
        Assert.Equal(string.Empty, viewModel.Password);
        Assert.Equal(DesktopSignInText.SignedOutMessage, viewModel.LastResult.Message);
        Assert.DoesNotContain("Desktop Operator", viewModel.LastResult.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ViewModelSubmitUsesCurrentBoundInputValues()
    {
        var password = CreateSensitiveValue("password");
        var deviceId = Guid.NewGuid();
        var signInService = new RecordingSignInService(DesktopSignInResult.Rejected("invalid_credentials"));
        var viewModel = new DesktopSignInViewModel(signInService)
        {
            Login = "stale-login",
            Password = CreateSensitiveValue("stale-password"),
            DeviceId = deviceId
        };

        viewModel.SetLoginInput("desktop-operator");
        viewModel.SetPasswordInput(password);

        _ = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(1, signInService.CallCount);
        Assert.Equal("desktop-operator", signInService.LastLogin);
        Assert.Equal(password, signInService.LastPassword);
        Assert.Equal(deviceId, signInService.LastDeviceId);
    }

    [Fact]
    public void DesktopShellUsesExplicitClickAndEnterSubmitWithoutNativeFormSubmit()
    {
        string markup = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "App.Desktop",
            "Components",
            "DesktopShell.razor"));

        Assert.DoesNotContain("<form", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@onsubmit", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("type=\"submit\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick:preventDefault", markup, StringComparison.Ordinal);
        Assert.Contains("type=\"button\"", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"SubmitSignInAsync\"", markup, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(markup, "@onkeyup=\"SubmitSignInOnEnterAsync\""));
        Assert.Contains("if (!string.Equals(args.Key, \"Enter\", StringComparison.Ordinal))", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopShellBindsInputsWithLiveInputEvents()
    {
        string markup = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "App.Desktop",
            "Components",
            "DesktopShell.razor"));

        Assert.Contains("@bind-value=\"SignInViewModel.Login\"", markup, StringComparison.Ordinal);
        Assert.Contains("@bind-value=\"SignInViewModel.Password\"", markup, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(markup, "@bind-value:event=\"oninput\""));
        Assert.DoesNotContain("@oninput=\"UpdateLogin\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@oninput=\"UpdatePassword\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopShellKeepsSubmitClickableWhileIdleAndLetsViewModelValidate()
    {
        string markup = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "App.Desktop",
            "Components",
            "DesktopShell.razor"));

        Assert.Contains("disabled=\"@SignInViewModel.IsBusy\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@(!SignInViewModel.CanSubmit)\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain(" required", markup, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(markup, "aria-required=\"true\""));
        Assert.Contains("if (SignInViewModel.IsBusy)", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("if (!SignInViewModel.CanSubmit)", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopShellSwitchesToSignedInWorkspaceWithDisabledNavigationPlaceholders()
    {
        string markup = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "App.Desktop",
            "Components",
            "DesktopShell.razor"));

        Assert.Contains("@if (SignInViewModel.IsSignedIn)", markup, StringComparison.Ordinal);
        Assert.Contains("@DesktopSignedInShellText.Title", markup, StringComparison.Ordinal);
        Assert.Contains("@SignInViewModel.SignedInUserContextMessage", markup, StringComparison.Ordinal);
        Assert.Contains("@DesktopSignedInShellText.SignOutButton", markup, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"SignOutAsync\"", markup, StringComparison.Ordinal);
        Assert.Contains("DesktopSignedInShellText.NavigationCards", markup, StringComparison.Ordinal);
        Assert.Contains("disabled=\"disabled\"", markup, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"true\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<UploadPlaceholder", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("IDesktopUploadOrchestrator", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("IControlPlaneApiClient", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("IDesktopFilePicker", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("PreUploadCheck", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SignedInShellTextDefinesDeferredRussianNavigationCards()
    {
        Assert.Equal("Рабочая область", DesktopSignedInShellText.Title);
        Assert.Equal("Выйти", DesktopSignedInShellText.SignOutButton);
        Assert.Equal(
            "Будет доступно в следующем approved desktop slice.",
            DesktopSignedInShellText.DeferredPlaceholderMessage);

        Assert.Collection(
            DesktopSignedInShellText.NavigationCards,
            card => AssertPlaceholderCard(card, "Группы"),
            card => AssertPlaceholderCard(card, "Загрузка видео"),
            card => AssertPlaceholderCard(card, "Проверка перед загрузкой"),
            card => AssertPlaceholderCard(card, "Квитанции загрузки"));
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
        Assert.Contains("Логин или пароль не приняты", result.Message, StringComparison.Ordinal);
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
        Assert.Contains("Сервис входа недоступен", result.Message, StringComparison.Ordinal);
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
        Assert.Equal("Вход выполнен.", result.Message);
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

    [Theory]
    [MemberData(nameof(ViewModelAttemptResults))]
    public async Task ViewModelPreservesLoginClearsPasswordAndKeepsVisibleResultAfterAttempt(
        DesktopSignInResult signInResult,
        DesktopSignInStatus expectedStatus,
        string expectedMessage)
    {
        const string login = "desktop-operator";
        var password = CreateSensitiveValue("password");
        var viewModel = new DesktopSignInViewModel(new RecordingSignInService(signInResult))
        {
            Login = login,
            Password = password
        };

        DesktopSignInResult result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Equal(result, viewModel.LastResult);
        Assert.Equal(login, viewModel.Login);
        Assert.Equal(string.Empty, viewModel.Password);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.LastResult.Message));
        Assert.DoesNotContain(password, viewModel.LastResult.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModelCanSubmitRequiresLoginPasswordAndIdleState()
    {
        var viewModel = new DesktopSignInViewModel(new RecordingSignInService(DesktopSignInResult.InProgress));

        Assert.False(viewModel.CanSubmit);

        viewModel.SetLoginInput("desktop-operator");

        Assert.False(viewModel.CanSubmit);

        viewModel.SetPasswordInput(CreateSensitiveValue("password"));

        Assert.True(viewModel.CanSubmit);
    }

    [Fact]
    public async Task ViewModelMissingCredentialsRejectsWithoutCallingSignInServiceAndPreservesLogin()
    {
        var signInService = new RecordingSignInService(DesktopSignInResult.Succeeded(
            DesktopSessionSnapshot.FromSession(CreateAuthenticatedSession())));
        const string login = "desktop-operator";
        var viewModel = new DesktopSignInViewModel(signInService)
        {
            Login = login,
            Password = " "
        };

        DesktopSignInResult result = await viewModel.SignInAsync(CancellationToken.None);

        Assert.Equal(DesktopSignInStatus.Rejected, result.Status);
        Assert.Equal(DesktopSignInText.NotStartedMessage, result.Message);
        Assert.Equal(result, viewModel.LastResult);
        Assert.Equal(0, signInService.CallCount);
        Assert.Equal(login, viewModel.Login);
        Assert.Equal(string.Empty, viewModel.Password);
        Assert.False(viewModel.IsBusy);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData(" ", "password")]
    [InlineData("desktop-operator", "")]
    [InlineData("desktop-operator", " ")]
    public void ViewModelCanSubmitRejectsMissingOrWhitespaceInput(string login, string password)
    {
        var viewModel = new DesktopSignInViewModel(new RecordingSignInService(DesktopSignInResult.InProgress));

        viewModel.SetLoginInput(login);
        viewModel.SetPasswordInput(password);

        Assert.False(viewModel.CanSubmit);
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
        Assert.Equal(DesktopSignInText.NotStartedMessage, result.Message);
    }

    [Fact]
    public void VisibleSignInTextUsesRussianLabelsAndActions()
    {
        Assert.Equal("Вход", DesktopSignInText.HeaderStatus);
        Assert.Equal("Выполняется вход", DesktopSignInText.HeaderStatusBusy);
        Assert.Equal("Вход выполнен", DesktopSignInText.HeaderStatusSignedIn);
        Assert.Equal("Вход в систему", DesktopSignInText.Title);
        Assert.Equal("Логин", DesktopSignInText.LoginLabel);
        Assert.Equal("Введите логин", DesktopSignInText.LoginPlaceholder);
        Assert.Equal("Пароль", DesktopSignInText.PasswordLabel);
        Assert.Equal("Введите пароль", DesktopSignInText.PasswordPlaceholder);
        Assert.Equal("Войти", DesktopSignInText.SubmitButton);
        Assert.Equal("Выполняется вход...", DesktopSignInText.SubmitButtonBusy);
        Assert.Equal("Вы вышли из системы. Введите логин и пароль для входа.", DesktopSignInText.SignedOutMessage);
    }

    public static IEnumerable<object[]> FailedAuthResults()
    {
        yield return [DesktopAuthResult.Rejected("invalid_credentials", "Rejected.")];
        yield return [DesktopAuthResult.Failed("sign_in_failed", "Failed.")];
        yield return [DesktopAuthResult.Unavailable("sign_in_unavailable", "Unavailable.")];
    }

    public static IEnumerable<object[]> ViewModelAttemptResults()
    {
        yield return
        [
            DesktopSignInResult.Rejected("invalid_credentials"),
            DesktopSignInStatus.Rejected,
            DesktopSignInText.RejectedMessage
        ];
        yield return
        [
            DesktopSignInResult.Unavailable("sign_in_unavailable"),
            DesktopSignInStatus.Unavailable,
            DesktopSignInText.UnavailableMessage
        ];
        yield return
        [
            DesktopSignInResult.Failed("sign_in_failed"),
            DesktopSignInStatus.Failed,
            DesktopSignInText.FailedMessage
        ];

        var session = CreateAuthenticatedSession(displayName: "Desktop Operator");
        yield return
        [
            DesktopSignInResult.Succeeded(DesktopSessionSnapshot.FromSession(session)),
            DesktopSignInStatus.Succeeded,
            DesktopSignInText.CreateSuccessMessage(DesktopSessionSnapshot.FromSession(session))
        ];
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AnalyticsAutomation-Core.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private static int CountOccurrences(string value, string match)
    {
        int count = 0;
        int startIndex = 0;

        while ((startIndex = value.IndexOf(match, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += match.Length;
        }

        return count;
    }

    private static void AssertPlaceholderCard(
        DesktopNavigationPlaceholderCard card,
        string expectedTitle)
    {
        Assert.Equal(expectedTitle, card.Title);
        Assert.Equal(DesktopSignedInShellText.DeferredPlaceholderMessage, card.Message);
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
        public int CallCount { get; private set; }

        public string? LastLogin { get; private set; }

        public string? LastPassword { get; private set; }

        public Guid? LastDeviceId { get; private set; }

        public ValueTask<DesktopSignInResult> SignInAsync(
            string login,
            string password,
            Guid? deviceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;
            LastLogin = login;
            LastPassword = password;
            LastDeviceId = deviceId;

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