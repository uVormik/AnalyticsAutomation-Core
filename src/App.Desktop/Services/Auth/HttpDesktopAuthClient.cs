using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using App.Desktop.Boundaries;

namespace App.Desktop.Services.Auth;

public sealed class HttpDesktopAuthClient : IDesktopAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _signInEndpoint;

    public HttpDesktopAuthClient(HttpClient httpClient)
        : this(httpClient, new Uri("/api/auth/sign-in", UriKind.Relative))
    {
    }

    public HttpDesktopAuthClient(HttpClient httpClient, Uri signInEndpoint)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(signInEndpoint);

        _httpClient = httpClient;
        _signInEndpoint = signInEndpoint;
    }

    public async ValueTask<DesktopAuthResult> SignInAsync(
        DesktopSignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var response = await _httpClient.PostAsJsonAsync(
            _signInEndpoint,
            new SignInRequestPayload(request.Login, request.Password, request.DeviceId),
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return DesktopAuthResult.Rejected(
                "invalid_credentials",
                "Control-plane sign-in rejected the supplied credentials.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return DesktopAuthResult.Failed(
                "sign_in_http_error",
                $"Control-plane sign-in failed with HTTP {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SignInResponsePayload>(
            JsonOptions,
            cancellationToken);

        if (payload?.User is null
            || payload.Session is null
            || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            return DesktopAuthResult.Failed(
                "sign_in_invalid_response",
                "Control-plane sign-in returned an invalid session response.");
        }

        var displayName = string.IsNullOrWhiteSpace(payload.User.DisplayName)
            ? payload.User.UserName
            : payload.User.DisplayName;

        var session = DesktopAuthenticatedSession.Create(
            payload.Session.SessionId,
            payload.User.UserId,
            payload.Session.DeviceId,
            displayName,
            payload.AccessToken,
            payload.RefreshToken,
            payload.Session.IssuedAtUtc,
            payload.Session.ExpiresAtUtc,
            payload.Session.IsOfflineRestricted);

        return DesktopAuthResult.Succeeded(session);
    }

    private sealed record SignInRequestPayload(
        string Login,
        string Password,
        Guid? DeviceId);

    private sealed record SignInResponsePayload(
        CurrentUserPayload User,
        SessionPayload Session,
        string? AccessToken,
        string? RefreshToken);

    private sealed record CurrentUserPayload(
        Guid UserId,
        string UserName,
        string DisplayName);

    private sealed record SessionPayload(
        Guid SessionId,
        Guid UserId,
        Guid? DeviceId,
        bool IsOfflineRestricted,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);
}