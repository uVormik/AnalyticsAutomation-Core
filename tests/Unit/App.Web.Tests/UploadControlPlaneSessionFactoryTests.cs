using System.Globalization;

using App.Web.Features.Upload.ControlPlane;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadControlPlaneSessionFactoryTests
{
    [Fact]
    public void CreateSessionRequiresAccessToken()
    {
        var response = new UploadControlPlaneSignInResponse();

        Assert.Throws<InvalidOperationException>(() =>
            UploadControlPlaneSessionFactory.CreateSession(response, ParseUtc("2026-04-22T12:00:00Z")));
    }

    [Fact]
    public void CreateSessionUsesEffectiveUserSummaryWithoutExposingTokenValuesInSanitizedShape()
    {
        var accessToken = new string('a', 16);
        var refreshToken = new string('b', 16);
        var response = new UploadControlPlaneSignInResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UploadControlPlaneUserSummary
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DisplayName = "Integration Web"
            }
        };

        var session = UploadControlPlaneSessionFactory.CreateSession(response, ParseUtc("2026-04-22T12:00:00Z"));
        var sanitized = session.ToSanitized();
        var serialized = sanitized.ToString();

        Assert.Equal(response.User.UserId, sanitized.UserId);
        Assert.True(sanitized.HasAccessToken);
        Assert.True(sanitized.HasRefreshToken);
        Assert.DoesNotContain(accessToken, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateSignInRequestRequiresLoginAndPassword()
    {
        var form = new UploadControlPlaneSignInFormModel();

        Assert.Throws<InvalidOperationException>(() =>
            UploadControlPlaneSessionFactory.CreateSignInRequest(form));
    }

    [Fact]
    public void CreateSignInRequestTrimsLogin()
    {
        var form = new UploadControlPlaneSignInFormModel
        {
            Login = " integration-user ",
            Password = new string('p', 8)
        };

        var request = UploadControlPlaneSessionFactory.CreateSignInRequest(form);

        Assert.Equal("integration-user", request.Login);
        Assert.NotEmpty(request.Password);
    }

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}