using System.Net;
using System.Text;
using System.Text.Json;

using App.Desktop.Boundaries;
using App.Desktop.Services.Auth;

namespace App.Desktop.Tests;

public sealed class DesktopAuthClientBoundaryTests
{
    [Fact]
    public async Task UnavailableAuthClientReturnsSafeFailureWithoutCredentialValues()
    {
        var client = new UnavailableDesktopAuthClient();
        var password = CreateSensitiveValue("password");
        var request = new DesktopSignInRequest("desktop-operator", password, Guid.NewGuid());

        var result = await client.SignInAsync(request, CancellationToken.None);

        Assert.Equal(DesktopAuthStatus.Unavailable, result.Status);
        Assert.Null(result.Session);
        Assert.NotNull(result.Error);
        Assert.DoesNotContain("desktop-operator", result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("desktop-operator", request.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, request.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpAuthClientUsesExistingSignInEndpointAndReturnsSession()
    {
        var accessToken = CreateSensitiveValue("access");
        var refreshToken = CreateSensitiveValue("refresh");
        var deviceId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new
            {
                user = new
                {
                    userId = Guid.NewGuid(),
                    userName = "desktop-operator",
                    displayName = "Desktop Operator"
                },
                session = new
                {
                    sessionId = Guid.NewGuid(),
                    userId = Guid.NewGuid(),
                    deviceId,
                    isOfflineRestricted = false,
                    issuedAtUtc = DateTimeOffset.UtcNow,
                    expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                },
                accessToken,
                refreshToken
            })
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://control-plane.local")
        };
        var client = new HttpDesktopAuthClient(httpClient);

        var result = await client.SignInAsync(
            new DesktopSignInRequest("desktop-operator", CreateSensitiveValue("password"), deviceId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DesktopAuthStatus.Succeeded, result.Status);
        Assert.Equal(accessToken, result.Session!.AccessToken);
        Assert.Equal(refreshToken, result.Session.RefreshToken);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("/api/auth/sign-in", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.DoesNotContain(accessToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, result.Session.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, result.Session.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpAuthFailureDoesNotExposeCredentialsOrTokenLikeResponseValues()
    {
        var password = CreateSensitiveValue("password");
        var responseToken = CreateSensitiveValue("access");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent(new
            {
                password,
                accessToken = responseToken
            })
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://control-plane.local")
        };
        var client = new HttpDesktopAuthClient(httpClient);

        var result = await client.SignInAsync(
            new DesktopSignInRequest("desktop-operator", password, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(DesktopAuthStatus.Rejected, result.Status);
        Assert.Null(result.Session);
        Assert.DoesNotContain("desktop-operator", result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(responseToken, result.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(password, result.Error!.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(responseToken, result.Error.Message, StringComparison.Ordinal);
    }

    private static StringContent JsonContent<T>(T value)
    {
        return new StringContent(
            JsonSerializer.Serialize(value),
            Encoding.UTF8,
            "application/json");
    }

    private static string CreateSensitiveValue(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) :
        HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}