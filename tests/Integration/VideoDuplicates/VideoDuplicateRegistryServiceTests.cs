using BuildingBlocks.Contracts.VideoDuplicates;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.VideoDuplicates;

namespace VideoDuplicates.IntegrationTests;

public sealed class VideoDuplicateRegistryServiceTests
{
    [Fact]
    public async Task RegisterAssetAsyncRegistersFirstAssetWithoutCandidates()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDuplicateRegistryService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var result = await service.RegisterAssetAsync(
            CreateRequest(Guid.NewGuid(), "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "external-1"),
            CancellationToken.None);

        Assert.True(result.Registered);
        Assert.Empty(result.Candidates);
        Assert.Equal(1, await db.VideoDuplicateAssets.CountAsync());
        Assert.Equal(1, await db.VideoDuplicateFingerprints.CountAsync());
        Assert.Equal(1, await db.VideoDuplicateRegistryAuditRecords.CountAsync());
    }

    [Fact]
    public async Task RegisterAssetAsyncCreatesHardDuplicateCandidateForExactFingerprint()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDuplicateRegistryService>();

        var hash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        var first = await service.RegisterAssetAsync(CreateRequest(Guid.NewGuid(), hash, "external-2"), CancellationToken.None);
        var second = await service.RegisterAssetAsync(CreateRequest(Guid.NewGuid(), hash, "external-3"), CancellationToken.None);

        Assert.True(first.Registered);
        Assert.True(second.Registered);
        Assert.Single(second.Candidates);
        Assert.Equal(DuplicateMatchKinds.HardDuplicate, second.Candidates.Single().MatchKind);
        Assert.Equal(first.VideoAssetId, second.Candidates.Single().MatchedVideoAssetId);
    }

    [Fact]
    public async Task RegisterAssetAsyncIsIdempotentByUploadReceiptId()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVideoDuplicateRegistryService>();

        var receiptId = Guid.NewGuid();
        var request = CreateRequest(receiptId, "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc", "external-4");

        var first = await service.RegisterAssetAsync(request, CancellationToken.None);
        var second = await service.RegisterAssetAsync(request, CancellationToken.None);

        Assert.True(first.Registered);
        Assert.False(second.Registered);
        Assert.Equal(first.VideoAssetId, second.VideoAssetId);
    }

    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:VideoDuplicates:RegistryEnabled"] = "true",
            ["Modules:VideoDuplicates:ExactFingerprintEnabled"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddVideoDuplicatesModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static DuplicateAssetRegistrationRequestDto CreateRequest(
        Guid receiptId,
        string hash,
        string externalVideoId)
    {
        return new DuplicateAssetRegistrationRequestDto(
            UploadReceiptId: receiptId,
            PreUploadCheckId: Guid.NewGuid(),
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DeviceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            GroupNodeId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            BusinessObjectKey: "demo-business-object",
            ExternalVideoId: externalVideoId,
            StorageKey: $"videos/{externalVideoId}.mp4",
            SizeBytes: 12345,
            ByteSha256: hash,
            UploadedAtUtc: DateTimeOffset.Parse(
                "2026-04-18T00:00:00+00:00",
                System.Globalization.CultureInfo.InvariantCulture));
    }
}