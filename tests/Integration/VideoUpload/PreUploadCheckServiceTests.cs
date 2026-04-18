using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.VideoUpload;

namespace VideoUpload.IntegrationTests;

public sealed class PreUploadCheckServiceTests
{
    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:VideoUpload:PreUploadCheckEnabled"] = "true",
            ["Modules:VideoUpload:MaxFastAllowSizeBytes"] = "5368709120",
            ["Modules:VideoUpload:SiteProvider"] = "Stub",
            ["Modules:VideoUpload:ExternalVideoIdPrefix"] = "site-video",
            ["Modules:VideoUpload:StorageKeyPrefix"] = "videos"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddVideoUploadModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    [Fact]
    public async Task CheckAsyncAllowsNewFingerprint()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IPreUploadCheckService>();

        var result = await service.CheckAsync(
            CreateRequest("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            CancellationToken.None);

        Assert.Equal(PreUploadCheckDecisions.Allow, result.Decision);
        Assert.True(result.CanUploadToSite);
        Assert.NotNull(result.SitePlan);
    }

    [Fact]
    public async Task CheckAsyncBlocksHardDuplicateForExactFingerprint()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IPreUploadCheckService>();
        var request = CreateRequest("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

        _ = await service.CheckAsync(request, CancellationToken.None);
        var duplicate = await service.CheckAsync(request, CancellationToken.None);

        Assert.Equal(PreUploadCheckDecisions.BlockHardDuplicate, duplicate.Decision);
        Assert.False(duplicate.CanUploadToSite);
    }

    [Fact]
    public async Task CheckAsyncAllowsWithReviewWhenContextIsIncomplete()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IPreUploadCheckService>();

        var request = CreateRequest("cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc") with
        {
            DeviceId = null,
            GroupNodeId = null
        };

        var result = await service.CheckAsync(request, CancellationToken.None);

        Assert.Equal(PreUploadCheckDecisions.AllowWithReview, result.Decision);
        Assert.True(result.CanUploadToSite);
    }

    [Fact]
    public async Task CheckAsyncBlocksPossibleFalsificationAfterRepeatedDuplicateBlock()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IPreUploadCheckService>();
        var request = CreateRequest("dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd");

        _ = await service.CheckAsync(request, CancellationToken.None);
        _ = await service.CheckAsync(request, CancellationToken.None);
        var repeated = await service.CheckAsync(request, CancellationToken.None);

        Assert.Equal(PreUploadCheckDecisions.BlockPossibleFalsification, repeated.Decision);
        Assert.False(repeated.CanUploadToSite);
    }

    private static VideoPreUploadCheckRequestDto CreateRequest(string hash)
    {
        return new VideoPreUploadCheckRequestDto(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DeviceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            GroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            BusinessObjectKey: "demo-business-object",
            FileName: "demo.mp4",
            SizeBytes: 12345,
            ByteSha256: hash,
            ContentType: "video/mp4",
            CapturedAtUtc: DateTimeOffset.Parse(
                "2026-04-18T00:00:00+00:00",
                System.Globalization.CultureInfo.InvariantCulture));
    }
}