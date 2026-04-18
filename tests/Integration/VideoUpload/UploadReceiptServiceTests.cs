using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.VideoUpload;

namespace VideoUpload.IntegrationTests;

public sealed class UploadReceiptServiceTests
{
    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:VideoUpload:PreUploadCheckEnabled"] = "true",
            ["Modules:VideoUpload:UploadReceiptEnabled"] = "true",
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
    public async Task AcceptAsyncStoresReceiptQueuesJobAndAudit()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var precheck = scope.ServiceProvider.GetRequiredService<IPreUploadCheckService>();
        var receipts = scope.ServiceProvider.GetRequiredService<IUploadReceiptService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var precheckResult = await precheck.CheckAsync(CreatePrecheckRequest("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), CancellationToken.None);
        var receipt = await receipts.AcceptAsync(CreateReceiptRequest(precheckResult, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "idem-1"), CancellationToken.None);

        Assert.Equal(VideoUploadReceiptStatuses.Accepted, receipt.Status);
        Assert.True(receipt.Accepted);
        Assert.False(receipt.WasAlreadyAccepted);
        Assert.Equal(1, await db.VideoUploadReceipts.CountAsync());
        Assert.Equal(1, await db.VideoUploadReceiptAnalysisJobs.CountAsync());
        Assert.Equal(1, await db.VideoUploadReceiptAuditRecords.CountAsync());
    }

    [Fact]
    public async Task AcceptAsyncIsIdempotentByKey()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var precheck = scope.ServiceProvider.GetRequiredService<IPreUploadCheckService>();
        var receipts = scope.ServiceProvider.GetRequiredService<IUploadReceiptService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var hash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        var precheckResult = await precheck.CheckAsync(CreatePrecheckRequest(hash), CancellationToken.None);

        var first = await receipts.AcceptAsync(CreateReceiptRequest(precheckResult, hash, "idem-2"), CancellationToken.None);
        var second = await receipts.AcceptAsync(CreateReceiptRequest(precheckResult, hash, "idem-2"), CancellationToken.None);

        Assert.Equal(first.UploadReceiptId, second.UploadReceiptId);
        Assert.True(second.WasAlreadyAccepted);
        Assert.Equal(VideoUploadReceiptStatuses.AlreadyAccepted, second.Status);
        Assert.Equal(1, await db.VideoUploadReceipts.CountAsync());
        Assert.Equal(1, await db.VideoUploadReceiptAnalysisJobs.CountAsync());
    }

    [Fact]
    public async Task AcceptAsyncRejectsUnknownPreUploadCheck()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var receipts = scope.ServiceProvider.GetRequiredService<IUploadReceiptService>();

        var request = new VideoUploadReceiptRequestDto(
            PreUploadCheckId: Guid.NewGuid(),
            UserId: UserId,
            DeviceId: DeviceId,
            GroupNodeId: GroupNodeId,
            ExternalVideoId: "site-video-missing",
            StorageKey: "videos/site-video-missing.mp4",
            SiteStatus: "uploaded",
            SizeBytes: 12345,
            ByteSha256: "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            IdempotencyKey: "idem-3",
            UploadedAtUtc: FixedTime);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => receipts.AcceptAsync(request, CancellationToken.None));
    }

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DeviceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GroupNodeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset FixedTime = DateTimeOffset.Parse(
        "2026-04-18T00:00:00+00:00",
        System.Globalization.CultureInfo.InvariantCulture);

    private static VideoPreUploadCheckRequestDto CreatePrecheckRequest(string hash)
    {
        return new VideoPreUploadCheckRequestDto(
            UserId: UserId,
            DeviceId: DeviceId,
            GroupNodeId: GroupNodeId,
            BusinessObjectKey: "demo-business-object",
            FileName: "demo.mp4",
            SizeBytes: 12345,
            ByteSha256: hash,
            ContentType: "video/mp4",
            CapturedAtUtc: FixedTime);
    }

    private static VideoUploadReceiptRequestDto CreateReceiptRequest(
        VideoPreUploadCheckResponseDto precheck,
        string hash,
        string idempotencyKey)
    {
        return new VideoUploadReceiptRequestDto(
            PreUploadCheckId: precheck.PreUploadCheckId,
            UserId: UserId,
            DeviceId: DeviceId,
            GroupNodeId: GroupNodeId,
            ExternalVideoId: precheck.SitePlan!.ExternalVideoId,
            StorageKey: precheck.SitePlan.StorageKey,
            SiteStatus: "uploaded",
            SizeBytes: 12345,
            ByteSha256: hash,
            IdempotencyKey: idempotencyKey,
            UploadedAtUtc: FixedTime);
    }
}