using BuildingBlocks.Contracts.VideoDownload;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.VideoDownload;

namespace VideoDownload.IntegrationTests;

public sealed class VideoDownloadServiceTests
{
    [Fact]
    public async Task CreateDownloadIntentAsyncCreatesIntent()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDownloadService>();
        var intent = await service.CreateDownloadIntentAsync(CreateIntentRequest(), CancellationToken.None);

        Assert.Equal(DownloadIntentV1Statuses.Created, intent.Status);
        Assert.Contains("site-video-1001", intent.DownloadUrl, StringComparison.Ordinal);
        Assert.Equal("Stub", intent.SiteProvider);
    }

    [Fact]
    public async Task AcceptDownloadReceiptAsyncConsumesIntent()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDownloadService>();
        var intent = await service.CreateDownloadIntentAsync(CreateIntentRequest(), CancellationToken.None);

        var receipt = await service.AcceptDownloadReceiptAsync(
            CreateReceiptRequest(intent.DownloadIntentId, intent.UserId, intent.DeviceId, intent.ExternalVideoId),
            CancellationToken.None);

        var updatedIntent = await service.GetIntentAsync(intent.DownloadIntentId, CancellationToken.None);

        Assert.Equal(DownloadReceiptV1Statuses.Accepted, receipt.Status);
        Assert.False(receipt.DuplicateIgnored);
        Assert.Equal(DownloadIntentV1Statuses.Consumed, updatedIntent!.Status);
    }

    [Fact]
    public async Task AcceptDownloadReceiptAsyncIsIdempotentByClientReceiptKey()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDownloadService>();
        var intent = await service.CreateDownloadIntentAsync(CreateIntentRequest(), CancellationToken.None);

        var first = await service.AcceptDownloadReceiptAsync(
            CreateReceiptRequest(intent.DownloadIntentId, intent.UserId, intent.DeviceId, intent.ExternalVideoId, "receipt-key-1"),
            CancellationToken.None);

        var second = await service.AcceptDownloadReceiptAsync(
            CreateReceiptRequest(intent.DownloadIntentId, intent.UserId, intent.DeviceId, intent.ExternalVideoId, "receipt-key-1"),
            CancellationToken.None);

        Assert.Equal(first.DownloadReceiptId, second.DownloadReceiptId);
        Assert.Equal(DownloadReceiptV1Statuses.DuplicateIgnored, second.Status);
        Assert.True(second.DuplicateIgnored);
    }

    [Fact]
    public async Task AcceptDownloadReceiptAsyncRejectsMismatchedExternalVideoId()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDownloadService>();
        var intent = await service.CreateDownloadIntentAsync(CreateIntentRequest(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AcceptDownloadReceiptAsync(
                CreateReceiptRequest(intent.DownloadIntentId, intent.UserId, intent.DeviceId, "wrong-site-video"),
                CancellationToken.None));
    }

    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:VideoDownload:Enabled"] = "true",
            ["Modules:VideoDownload:DefaultIntentTtlSeconds"] = "900",
            ["Modules:VideoDownload:SiteProvider"] = "Stub",
            ["Modules:VideoDownload:StubDownloadBaseUrl"] = "https://stub-video-site.local/download"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddVideoDownloadModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static CreateDownloadIntentRequestV1Dto CreateIntentRequest()
    {
        return new CreateDownloadIntentRequestV1Dto(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DeviceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            GroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            VideoAssetId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            ExternalVideoId: "site-video-1001",
            BusinessObjectKey: "download-business-object",
            ExpiresInSeconds: 900);
    }

    private static DownloadReceiptRequestV1Dto CreateReceiptRequest(
        Guid downloadIntentId,
        Guid userId,
        Guid? deviceId,
        string externalVideoId,
        string? clientReceiptKey = "receipt-key-1")
    {
        return new DownloadReceiptRequestV1Dto(
            DownloadIntentId: downloadIntentId,
            UserId: userId,
            DeviceId: deviceId,
            ExternalVideoId: externalVideoId,
            ClientReceiptKey: clientReceiptKey,
            DownloadedBytes: 1024,
            DownloadedAtUtc: DateTimeOffset.Parse(
                "2026-04-18T00:00:00+00:00",
                System.Globalization.CultureInfo.InvariantCulture));
    }
}