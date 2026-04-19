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

    [Fact]
    public async Task GroupTreeNodesRejectsAnonymous()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/group-tree/nodes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
