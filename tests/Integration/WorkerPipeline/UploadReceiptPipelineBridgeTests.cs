using System.Globalization;

using BuildingBlocks.Contracts.WorkerPipeline;
using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Modules.GroupTree;
using Modules.Incidents;
using Modules.VideoDuplicates;
using Modules.VideoUpload;
using Modules.WorkerPipeline;

namespace WorkerPipeline.IntegrationTests;

public sealed class UploadReceiptPipelineBridgeTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DeviceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchGroupNodeId = Guid.Parse("D4D74008-0ED5-4E46-B0A1-91E0628079C0");
    private static readonly Guid RootGroupNodeId = Guid.Parse("C15EE7FE-7C9A-4B31-8B7E-C278B59F318C");
    private static readonly Guid BranchAdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset FixedTime = DateTimeOffset.Parse(
        "2026-04-18T00:00:00+00:00",
        CultureInfo.InvariantCulture);

    [Fact]
    public async Task ProcessNextQueuedAsyncMarksQueuedJobAndReceiptCompletedAndRegistersAsset()
    {
        using var provider = CreateProvider();

        var accepted = await AcceptReceiptAsync(
            provider,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "bridge-idem-1");

        using var scope = provider.CreateScope();
        var bridge = scope.ServiceProvider.GetRequiredService<IUploadReceiptPipelineBridgeService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var processed = await bridge.ProcessNextQueuedAsync(CancellationToken.None);
        var receipt = await db.VideoUploadReceipts.SingleAsync(item => item.Id == accepted.UploadReceiptId);
        var job = await db.VideoUploadReceiptAnalysisJobs.SingleAsync(item => item.Id == accepted.AnalysisJobId);

        Assert.True(processed.Processed);
        Assert.Equal(accepted.AnalysisJobId, processed.AnalysisJobId);
        Assert.Equal(accepted.UploadReceiptId, processed.UploadReceiptId);
        Assert.Equal(WorkerPipelineJobStatusesV1.Completed, processed.JobStatus);
        Assert.Equal(WorkerPipelineJobStatusesV1.Completed, receipt.AnalysisJobStatus);
        Assert.Equal(WorkerPipelineJobStatusesV1.Completed, job.Status);
        Assert.Equal(1, await db.VideoDuplicateAssets.CountAsync());
        Assert.Equal(0, await db.VideoDuplicateCandidates.CountAsync());
        Assert.Equal(0, await db.DuplicateIncidentRecords.CountAsync());
        Assert.Equal(3, await db.VideoUploadReceiptAuditRecords.CountAsync());
    }

    [Fact]
    public async Task ProcessNextQueuedAsyncCreatesDuplicateCandidateAndIncidentWhenRoutingSupported()
    {
        using var provider = CreateProvider();

        await EnsureBranchAdminRoutingAsync(provider);

        await ProcessAcceptedReceiptAsync(
            provider,
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            "bridge-idem-2");

        var duplicate = await AcceptReceiptAsync(
            provider,
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            "bridge-idem-3");

        using var scope = provider.CreateScope();
        var bridge = scope.ServiceProvider.GetRequiredService<IUploadReceiptPipelineBridgeService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var processed = await bridge.ProcessNextQueuedAsync(CancellationToken.None);
        var candidate = await db.VideoDuplicateCandidates.SingleAsync();
        var incident = await db.DuplicateIncidentRecords.SingleAsync();
        var receipt = await db.VideoUploadReceipts.SingleAsync(item => item.Id == duplicate.UploadReceiptId);
        var job = await db.VideoUploadReceiptAnalysisJobs.SingleAsync(item => item.Id == duplicate.AnalysisJobId);

        Assert.True(processed.Processed);
        Assert.Single(processed.DuplicateCandidateIds);
        Assert.Single(processed.IncidentIds);
        Assert.Equal(candidate.Id, processed.DuplicateCandidateIds.Single());
        Assert.Equal(incident.Id, processed.IncidentIds.Single());
        Assert.Equal(candidate.Id, incident.DuplicateCandidateId);
        Assert.Equal(BranchAdminUserId, (await db.DuplicateIncidentAssignmentRecords.SingleAsync()).AssignedAdminUserId);
        Assert.Equal(WorkerPipelineJobStatusesV1.Completed, receipt.AnalysisJobStatus);
        Assert.Equal(WorkerPipelineJobStatusesV1.Completed, job.Status);
    }

    [Fact]
    public async Task ProcessAsyncIsIdempotentForCompletedJob()
    {
        using var provider = CreateProvider();

        await EnsureBranchAdminRoutingAsync(provider);

        await ProcessAcceptedReceiptAsync(
            provider,
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "bridge-idem-4");

        var duplicate = await AcceptReceiptAsync(
            provider,
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "bridge-idem-5");

        using var scope = provider.CreateScope();
        var bridge = scope.ServiceProvider.GetRequiredService<IUploadReceiptPipelineBridgeService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var first = await bridge.ProcessNextQueuedAsync(CancellationToken.None);
        var second = await bridge.ProcessAsync(duplicate.AnalysisJobId, CancellationToken.None);

        Assert.True(first.Processed);
        Assert.False(second.Processed);
        Assert.Equal("completed", second.JobStatus);
        Assert.Equal(2, await db.VideoDuplicateAssets.CountAsync());
        Assert.Equal(1, await db.VideoDuplicateCandidates.CountAsync());
        Assert.Equal(1, await db.DuplicateIncidentRecords.CountAsync());
        Assert.Equal(1, await db.DuplicateIncidentAssignmentRecords.CountAsync());
    }

    private static async Task<AcceptedReceipt> ProcessAcceptedReceiptAsync(
        ServiceProvider provider,
        string hash,
        string idempotencyKey)
    {
        var accepted = await AcceptReceiptAsync(provider, hash, idempotencyKey);

        using var scope = provider.CreateScope();
        var bridge = scope.ServiceProvider.GetRequiredService<IUploadReceiptPipelineBridgeService>();

        var processed = await bridge.ProcessNextQueuedAsync(CancellationToken.None);

        Assert.True(processed.Processed);

        return accepted;
    }

    private static async Task<AcceptedReceipt> AcceptReceiptAsync(
        ServiceProvider provider,
        string hash,
        string idempotencyKey)
    {
        using var scope = provider.CreateScope();
        var receipts = scope.ServiceProvider.GetRequiredService<IUploadReceiptService>();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var precheck = CreatePreUploadCheck(hash);
        db.VideoUploadPreUploadChecks.Add(precheck);
        await db.SaveChangesAsync(CancellationToken.None);

        var receipt = await receipts.AcceptAsync(
            CreateReceiptRequest(precheck, hash, idempotencyKey),
            CancellationToken.None);

        var job = await db.VideoUploadReceiptAnalysisJobs
            .SingleAsync(item => item.UploadReceiptId == receipt.UploadReceiptId);

        return new AcceptedReceipt(receipt.UploadReceiptId, job.Id);
    }

    private static async Task EnsureBranchAdminRoutingAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        await db.Database.EnsureCreatedAsync();

        if (!await db.GroupNodes.AnyAsync(item => item.Id == RootGroupNodeId))
        {
            db.GroupNodes.Add(new GroupNode
            {
                Id = RootGroupNodeId,
                ParentNodeId = null,
                Code = "root",
                Name = "Root",
                Depth = 0,
                IsActive = true
            });
        }

        if (!await db.GroupNodes.AnyAsync(item => item.Id == BranchGroupNodeId))
        {
            db.GroupNodes.Add(new GroupNode
            {
                Id = BranchGroupNodeId,
                ParentNodeId = RootGroupNodeId,
                Code = "branch-a",
                Name = "Branch A",
                Depth = 1,
                IsActive = true
            });
        }

        if (!await db.GroupAdminAssignments.AnyAsync(
                item => item.GroupNodeId == BranchGroupNodeId && item.UserId == BranchAdminUserId))
        {
            db.GroupAdminAssignments.Add(new GroupAdminAssignment
            {
                GroupNodeId = BranchGroupNodeId,
                UserId = BranchAdminUserId,
                AssignedAtUtc = FixedTime
            });
        }

        await db.SaveChangesAsync();
    }

    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:VideoUpload:PreUploadCheckEnabled"] = "true",
            ["Modules:VideoUpload:UploadReceiptEnabled"] = "true",
            ["Modules:VideoUpload:MaxFastAllowSizeBytes"] = "5368709120",
            ["Modules:VideoUpload:SiteProvider"] = "Stub",
            ["Modules:VideoUpload:ExternalVideoIdPrefix"] = "site-video",
            ["Modules:VideoUpload:StorageKeyPrefix"] = "videos",
            ["Modules:VideoDuplicates:RegistryEnabled"] = "true",
            ["Modules:VideoDuplicates:ExactFingerprintEnabled"] = "true",
            ["Modules:Incidents:DuplicateIncidentRoutingEnabled"] = "true",
            ["Modules:WorkerPipeline:DeepAnalysisEnabled"] = "true",
            ["Modules:WorkerPipeline:MaxAttempts"] = "3",
            ["Modules:WorkerPipeline:PollingIntervalMilliseconds"] = "1000",
            ["Modules:WorkerPipeline:RetryDelaySeconds"] = "10"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(databaseName));
        services.AddGroupTreeModule(configuration, new TestHostEnvironment());
        services.AddVideoUploadModule(configuration);
        services.AddVideoDuplicatesModule(configuration);
        services.AddIncidentsModule(configuration);
        services.AddWorkerPipelineModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static VideoUploadPreUploadCheck CreatePreUploadCheck(string hash)
    {
        var preUploadCheckId = Guid.NewGuid();
        var externalVideoId = $"site-video-{preUploadCheckId:N}";

        return new VideoUploadPreUploadCheck
        {
            Id = preUploadCheckId,
            UserId = UserId,
            DeviceId = DeviceId,
            GroupNodeId = BranchGroupNodeId,
            BusinessObjectKey = "demo-business-object",
            FileName = "demo.mp4",
            SizeBytes = 12345,
            ByteSha256 = hash,
            ContentType = "video/mp4",
            CapturedAtUtc = FixedTime,
            Decision = PreUploadCheckDecisions.Allow,
            CanUploadToSite = true,
            ReasonCode = "fast_path_clear",
            ExistingPreUploadCheckId = null,
            RequiredNextSteps = "UPLOAD_TO_SITE_DIRECT,SEND_UPLOAD_RECEIPT",
            SiteProvider = "Stub",
            ExternalVideoId = externalVideoId,
            StorageKey = $"videos/{externalVideoId}.mp4",
            CheckedAtUtc = FixedTime
        };
    }

    private static VideoUploadReceiptRequestDto CreateReceiptRequest(
        VideoUploadPreUploadCheck precheck,
        string hash,
        string idempotencyKey)
    {
        return new VideoUploadReceiptRequestDto(
            PreUploadCheckId: precheck.Id,
            UserId: UserId,
            DeviceId: DeviceId,
            GroupNodeId: BranchGroupNodeId,
            ExternalVideoId: precheck.ExternalVideoId!,
            StorageKey: precheck.StorageKey!,
            SiteStatus: "uploaded",
            SizeBytes: 12345,
            ByteSha256: hash,
            IdempotencyKey: idempotencyKey,
            UploadedAtUtc: FixedTime);
    }

    private sealed record AcceptedReceipt(Guid UploadReceiptId, Guid AnalysisJobId);

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "WorkerPipeline.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}