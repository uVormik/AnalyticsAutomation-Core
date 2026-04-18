using BuildingBlocks.Contracts.WorkerPipeline;
using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Modules.WorkerPipeline;

namespace WorkerPipeline.IntegrationTests;

public sealed class WorkerPipelineServiceTests
{
    [Fact]
    public async Task EnqueueAnalyzeUploadedVideoCreatesQueuedJob()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IWorkerPipelineService>();
        var job = await service.EnqueueAnalyzeUploadedVideoAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(WorkerPipelineJobTypesV1.AnalyzeUploadedVideo, job.JobType);
        Assert.Equal(WorkerPipelineJobStatusesV1.Queued, job.Status);
        Assert.Equal(0, job.Attempts);
    }

    [Fact]
    public async Task ProcessOneMovesQueuedJobToCompleted()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IWorkerPipelineService>();
        var queued = await service.EnqueueAnalyzeUploadedVideoAsync(CreateRequest(), CancellationToken.None);

        var processed = await service.ProcessOneAsync(CancellationToken.None);

        Assert.True(processed.Processed);
        Assert.NotNull(processed.Job);
        Assert.Equal(queued.JobId, processed.Job!.JobId);
        Assert.Equal(WorkerPipelineJobStatusesV1.Completed, processed.Job.Status);
        Assert.Equal(1, processed.Job.Attempts);
        Assert.NotNull(processed.Job.ResultJson);
    }

    [Fact]
    public async Task EnqueueAnalyzeUploadedVideoIsIdempotentByUploadReceiptId()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IWorkerPipelineService>();
        var uploadReceiptId = Guid.NewGuid();

        var first = await service.EnqueueAnalyzeUploadedVideoAsync(CreateRequest(uploadReceiptId), CancellationToken.None);
        var second = await service.EnqueueAnalyzeUploadedVideoAsync(CreateRequest(uploadReceiptId), CancellationToken.None);

        Assert.Equal(first.JobId, second.JobId);
    }

    [Fact]
    public async Task FailReturnsJobToQueuedBeforeMaxAttempts()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IWorkerPipelineService>();
        await service.EnqueueAnalyzeUploadedVideoAsync(CreateRequest(), CancellationToken.None);
        var leased = await service.LeaseNextAsync(CancellationToken.None);

        var failed = await service.FailAsync(leased!.JobId, "transient failure", CancellationToken.None);

        Assert.Equal(WorkerPipelineJobStatusesV1.Queued, failed.Status);
        Assert.Equal(1, failed.Attempts);
        Assert.Equal("transient failure", failed.LastError);
    }

    private static ServiceProvider CreateProvider()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Modules:WorkerPipeline:DeepAnalysisEnabled"] = "true",
            ["Modules:WorkerPipeline:MaxAttempts"] = "3",
            ["Modules:WorkerPipeline:PollingIntervalMilliseconds"] = "1000",
            ["Modules:WorkerPipeline:RetryDelaySeconds"] = "10"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddWorkerPipelineModule(configuration);

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    private static AnalyzeUploadedVideoJobRequestV1Dto CreateRequest(Guid? uploadReceiptId = null)
    {
        return new AnalyzeUploadedVideoJobRequestV1Dto(
            UploadReceiptId: uploadReceiptId ?? Guid.NewGuid(),
            VideoAssetId: Guid.NewGuid(),
            DuplicateCandidateId: null,
            FraudSignalId: null,
            ExternalVideoId: "external-video-1",
            BusinessObjectKey: "demo-business-object",
            RequestedAtUtc: DateTimeOffset.Parse(
                "2026-04-18T00:00:00+00:00",
                System.Globalization.CultureInfo.InvariantCulture));
    }
}