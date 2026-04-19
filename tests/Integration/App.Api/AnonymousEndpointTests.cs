using System.Net;

namespace App.Api.IntegrationTests;

public sealed class AnonymousEndpointTests(AppApiFactory factory) : IClassFixture<AppApiFactory>
{
    [Fact]
    public async Task HealthLiveAllowsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
