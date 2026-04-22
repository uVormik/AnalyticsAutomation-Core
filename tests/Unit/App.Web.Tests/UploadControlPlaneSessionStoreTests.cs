using System.Globalization;

using App.Web.Features.Upload.ControlPlane;

using Xunit;

namespace App.Web.Tests;

public sealed class UploadControlPlaneSessionStoreTests
{
    [Fact]
    public async Task StoreKeepsSessionUntilCleared()
    {
        var store = new InMemoryUploadControlPlaneSessionStore();
        var session = new UploadControlPlaneSession(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DisplayName: "Integration Web",
            AccessToken: "alpha-sensitive-value",
            RefreshToken: "beta-sensitive-value",
            CreatedAtUtc: DateTimeOffset.Parse("2026-04-22T12:00:00Z", CultureInfo.InvariantCulture));

        await store.SetAsync(session);

        var stored = await store.GetAsync();

        Assert.NotNull(stored);
        Assert.Equal(session.UserId, stored.UserId);
        Assert.Equal(session.DisplayName, stored.DisplayName);
        Assert.Equal(session.AccessToken, stored.AccessToken);

        await store.ClearAsync();

        Assert.Null(await store.GetAsync());
    }

    [Fact]
    public void SanitizedSessionDoesNotExposeTokenValues()
    {
        var session = new UploadControlPlaneSession(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DisplayName: "Integration Web",
            AccessToken: "alpha-sensitive-value",
            RefreshToken: "beta-sensitive-value",
            CreatedAtUtc: DateTimeOffset.Parse("2026-04-22T12:00:00Z", CultureInfo.InvariantCulture));

        var sanitized = session.ToSanitized();
        var serialized = sanitized.ToString();

        Assert.True(sanitized.HasAccessToken);
        Assert.True(sanitized.HasRefreshToken);
        Assert.DoesNotContain("alpha-sensitive-value", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("beta-sensitive-value", serialized, StringComparison.Ordinal);
    }
}