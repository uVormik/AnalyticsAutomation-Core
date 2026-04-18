using BuildingBlocks.Contracts.WebsiteIntegration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Modules.WebsiteIntegration;

namespace WebsiteIntegration.IntegrationTests;

public sealed class WebsiteIntegrationGatewayV1Tests
{
    private static ServiceProvider CreateProvider(Action<Dictionary<string, string?>>? configure = null)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:WebsiteIntegration:Provider"] = "Stub",
            ["Modules:WebsiteIntegration:StubMode"] = "Development",
            ["Modules:WebsiteIntegration:ExternalVideoIdPrefix"] = "site-video",
            ["Modules:WebsiteIntegration:StorageKeyPrefix"] = "videos"
        };

        configure?.Invoke(settings);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebsiteIntegrationModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    [Fact]
    public async Task GetContractsAsync_Returns_StatusLookup_And_KnownStatuses()
    {
        using var provider = CreateProvider();
        var gateway = provider.GetRequiredService<IWebsiteVideoGateway>();

        var result = await gateway.GetContractsAsync(CancellationToken.None);

        Assert.Equal("Stub", result.Provider);
        Assert.NotNull(result.StatusLookup);
        Assert.Contains("available", result.StatusLookup!.KnownStatuses);
    }

    [Fact]
    public async Task AcceptUploadReceiptPreviewAsync_Returns_Accepted_Receipt()
    {
        using var provider = CreateProvider();
        var gateway = provider.GetRequiredService<IWebsiteVideoGateway>();

        var result = await gateway.AcceptUploadReceiptPreviewAsync(
            new SiteUploadReceiptPreviewRequestDto(
                ExternalVideoId: "site-video-0001",
                StorageKey: null,
                SiteStatus: "uploaded",
                UploadedAtUtc: DateTimeOffset.Parse("2026-04-18T00:00:00+00:00")),
            CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal("site-video-0001", result.ExternalVideoId);
        Assert.Equal("videos/site-video-0001.mp4", result.StorageKey);
        Assert.Equal("uploaded", result.SiteStatus);
    }

    [Fact]
    public async Task CreateDownloadIntentPreviewAsync_Returns_Stub_DownloadUrl()
    {
        using var provider = CreateProvider();
        var gateway = provider.GetRequiredService<IWebsiteVideoGateway>();

        var result = await gateway.CreateDownloadIntentPreviewAsync(
            new SiteDownloadIntentPreviewRequestDto("site-video-0002"),
            CancellationToken.None);

        Assert.Equal("available", result.SiteStatus);
        Assert.Contains("stub-site.invalid", result.DownloadUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("videos/site-video-0002.mp4", result.StorageKey);
    }

    [Fact]
    public async Task ReconcileAsync_Returns_Distinct_Items()
    {
        using var provider = CreateProvider();
        var gateway = provider.GetRequiredService<IWebsiteVideoGateway>();

        var result = await gateway.ReconcileAsync(
            new ReconcileSiteVideosRequestDto(
                new[]
                {
                    "site-video-0003",
                    "site-video-0003",
                    "site-video-0004"
                }),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetContractsAsync_Throws_For_NonStub_Provider()
    {
        using var provider = CreateProvider(settings =>
        {
            settings["Modules:WebsiteIntegration:Provider"] = "Live";
        });

        var gateway = provider.GetRequiredService<IWebsiteVideoGateway>();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => gateway.GetContractsAsync(CancellationToken.None));
    }
}