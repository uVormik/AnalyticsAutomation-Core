using System.Text.Json;

using BuildingBlocks.Contracts.WorkerPipeline;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.WorkerPipeline;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.WorkerPipeline;

public sealed class WorkerPipelineOptions
{
    public const string SectionName = "Modules:WorkerPipeline";

    public bool DeepAnalysisEnabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 3;
    public int PollingIntervalMilliseconds { get; set; } = 1000;
    public int RetryDelaySeconds { get; set; } = 10;
}

public interface IWorkerPipelineService
{
    Task<WorkerPipelineJobV1Dto> EnqueueAnalyzeUploadedVideoAsync(
        AnalyzeUploadedVideoJobRequestV1Dto request,
        CancellationToken cancellationToken);

    Task<WorkerPipelineJobV1Dto?> LeaseNextAsync(CancellationToken cancellationToken);

    Task<WorkerPipelineProcessResultV1Dto> ProcessOneAsync(CancellationToken cancellationToken);

    Task<WorkerPipelineJobV1Dto> CompleteAsync(
        Guid jobId,
        string resultJson,
        CancellationToken cancellationToken);

    Task<WorkerPipelineJobV1Dto> FailAsync(
        Guid jobId,
        string error,
        CancellationToken cancellationToken);

    Task<WorkerPipelineJobV1Dto?> GetAsync(
        Guid jobId,
        CancellationToken cancellationToken);

    Task<WorkerPipelineJobListResponseV1Dto> ListRecentAsync(
        int take,
        CancellationToken cancellationToken);
}

public sealed class WorkerPipelineService(
    PlatformDbContext dbContext,
    IOptions<WorkerPipelineOptions> options,
    ILogger<WorkerPipelineService> logger) : IWorkerPipelineService
{
    public async Task<WorkerPipelineJobV1Dto> EnqueueAnalyzeUploadedVideoAsync(
        AnalyzeUploadedVideoJobRequestV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.DeepAnalysisEnabled)
        {
            throw new InvalidOperationException("Worker deep analysis pipeline is disabled.");
        }

        ValidateAnalyzeRequest(request);

        var existing = await dbContext.WorkerPipelineJobRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.JobType == WorkerPipelineJobTypesV1.AnalyzeUploadedVideo &&
                    item.UploadReceiptId == request.UploadReceiptId,
                cancellationToken);

        if (existing is not null)
        {
            return ToDto(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var job = new WorkerPipelineJobRecord
        {
            Id = Guid.NewGuid(),
            JobType = WorkerPipelineJobTypesV1.AnalyzeUploadedVideo,
            Status = WorkerPipelineJobStatusesV1.Queued,
            UploadReceiptId = request.UploadReceiptId,
            PayloadJson = JsonSerializer.Serialize(request),
            ResultJson = null,
            Attempts = 0,
            MaxAttempts = Math.Max(1, options.Value.MaxAttempts),
            LastError = null,
            CreatedAtUtc = now,
            AvailableAtUtc = now,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            FailedAtUtc = null
        };

        dbContext.WorkerPipelineJobRecords.Add(job);
        AddAudit(job.Id, "worker_job_queued", new
        {
            job.Id,
            job.JobType,
            job.UploadReceiptId
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Worker pipeline job queued. JobId={JobId} JobType={JobType} UploadReceiptId={UploadReceiptId}",
            job.Id,
            job.JobType,
            job.UploadReceiptId);

        return ToDto(job);
    }

    public async Task<WorkerPipelineJobV1Dto?> LeaseNextAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.DeepAnalysisEnabled)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        var job = await dbContext.WorkerPipelineJobRecords
            .Where(item => item.Status == WorkerPipelineJobStatusesV1.Queued && item.AvailableAtUtc <= now)
            .OrderBy(item => item.AvailableAtUtc)
            .ThenBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = WorkerPipelineJobStatusesV1.Running;
        job.Attempts += 1;
        job.StartedAtUtc = now;
        job.LastError = null;

        AddAudit(job.Id, "worker_job_started", new
        {
            job.Id,
            job.JobType,
            job.UploadReceiptId,
            job.Attempts
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Worker pipeline job started. JobId={JobId} JobType={JobType} Attempts={Attempts}",
            job.Id,
            job.JobType,
            job.Attempts);

        return ToDto(job);
    }

    public async Task<WorkerPipelineProcessResultV1Dto> ProcessOneAsync(CancellationToken cancellationToken)
    {
        var leased = await LeaseNextAsync(cancellationToken);
        if (leased is null)
        {
            return new WorkerPipelineProcessResultV1Dto(
                Processed: false,
                Job: null,
                Message: "No queued worker pipeline job is available.");
        }

        try
        {
            var resultJson = JsonSerializer.Serialize(new
            {
                analysisKind = "stub-deep-analysis",
                status = "completed",
                jobId = leased.JobId,
                jobType = leased.JobType,
                uploadReceiptId = leased.UploadReceiptId,
                completedAtUtc = DateTimeOffset.UtcNow
            });

            var completed = await CompleteAsync(
                leased.JobId,
                resultJson,
                cancellationToken);

            return new WorkerPipelineProcessResultV1Dto(
                Processed: true,
                Job: completed,
                Message: "Worker pipeline job completed.");
        }
        catch (Exception exception)
        {
            var failed = await FailAsync(
                leased.JobId,
                exception.Message,
                cancellationToken);

            return new WorkerPipelineProcessResultV1Dto(
                Processed: true,
                Job: failed,
                Message: "Worker pipeline job failed.");
        }
    }

    public async Task<WorkerPipelineJobV1Dto> CompleteAsync(
        Guid jobId,
        string resultJson,
        CancellationToken cancellationToken)
    {
        var job = await GetMutableJobAsync(jobId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        job.Status = WorkerPipelineJobStatusesV1.Completed;
        job.ResultJson = string.IsNullOrWhiteSpace(resultJson)
            ? "{}"
            : resultJson.Trim();
        job.CompletedAtUtc = now;
        job.FailedAtUtc = null;
        job.LastError = null;

        AddAudit(job.Id, "worker_job_completed", new
        {
            job.Id,
            job.JobType,
            job.UploadReceiptId,
            job.Attempts
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Worker pipeline job completed. JobId={JobId} JobType={JobType}",
            job.Id,
            job.JobType);

        return ToDto(job);
    }

    public async Task<WorkerPipelineJobV1Dto> FailAsync(
        Guid jobId,
        string error,
        CancellationToken cancellationToken)
    {
        var job = await GetMutableJobAsync(jobId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        job.LastError = string.IsNullOrWhiteSpace(error)
            ? "Worker pipeline job failed."
            : error.Trim();

        if (job.Attempts >= job.MaxAttempts)
        {
            job.Status = WorkerPipelineJobStatusesV1.Failed;
            job.FailedAtUtc = now;
        }
        else
        {
            job.Status = WorkerPipelineJobStatusesV1.Queued;
            job.AvailableAtUtc = now.AddSeconds(Math.Max(1, options.Value.RetryDelaySeconds));
        }

        AddAudit(job.Id, "worker_job_failed", new
        {
            job.Id,
            job.JobType,
            job.UploadReceiptId,
            job.Attempts,
            job.MaxAttempts,
            job.Status,
            job.LastError
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Worker pipeline job failed. JobId={JobId} JobType={JobType} Attempts={Attempts} Status={Status}",
            job.Id,
            job.JobType,
            job.Attempts,
            job.Status);

        return ToDto(job);
    }

    public async Task<WorkerPipelineJobV1Dto?> GetAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("JobId is required.", nameof(jobId));
        }

        var job = await dbContext.WorkerPipelineJobRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        return job is null ? null : ToDto(job);
    }

    public async Task<WorkerPipelineJobListResponseV1Dto> ListRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 100);

        var jobs = await dbContext.WorkerPipelineJobRecords
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        return new WorkerPipelineJobListResponseV1Dto(jobs.Select(ToDto).ToArray());
    }

    private async Task<WorkerPipelineJobRecord> GetMutableJobAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("JobId is required.", nameof(jobId));
        }

        var job = await dbContext.WorkerPipelineJobRecords
            .SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException("Worker pipeline job was not found.");
        }

        return job;
    }

    private void AddAudit(Guid jobId, string action, object payload)
    {
        dbContext.WorkerPipelineJobAuditRecords.Add(new WorkerPipelineJobAuditRecord
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Action = action,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private static WorkerPipelineJobV1Dto ToDto(WorkerPipelineJobRecord job)
    {
        return new WorkerPipelineJobV1Dto(
            JobId: job.Id,
            JobType: job.JobType,
            Status: job.Status,
            UploadReceiptId: job.UploadReceiptId,
            Attempts: job.Attempts,
            MaxAttempts: job.MaxAttempts,
            LastError: job.LastError,
            PayloadJson: job.PayloadJson,
            ResultJson: job.ResultJson,
            CreatedAtUtc: job.CreatedAtUtc,
            AvailableAtUtc: job.AvailableAtUtc,
            StartedAtUtc: job.StartedAtUtc,
            CompletedAtUtc: job.CompletedAtUtc,
            FailedAtUtc: job.FailedAtUtc);
    }

    private static void ValidateAnalyzeRequest(AnalyzeUploadedVideoJobRequestV1Dto request)
    {
        if (request.UploadReceiptId == Guid.Empty)
        {
            throw new ArgumentException("UploadReceiptId is required.", nameof(request));
        }
    }
}

public sealed class WorkerPipelineHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerPipelineOptions> options,
    ILogger<WorkerPipelineHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker pipeline hosted service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IWorkerPipelineService>();
                await service.ProcessOneAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Worker pipeline hosted service iteration failed.");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(Math.Max(250, options.Value.PollingIntervalMilliseconds)),
                stoppingToken);
        }
    }
}

public static class WorkerPipelineModule
{
    public static IServiceCollection AddWorkerPipelineModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<WorkerPipelineOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(WorkerPipelineOptions.SectionName);

                if (bool.TryParse(section["DeepAnalysisEnabled"], out var enabled))
                {
                    options.DeepAnalysisEnabled = enabled;
                }

                if (int.TryParse(section["MaxAttempts"], out var maxAttempts))
                {
                    options.MaxAttempts = maxAttempts;
                }

                if (int.TryParse(section["PollingIntervalMilliseconds"], out var pollingIntervalMilliseconds))
                {
                    options.PollingIntervalMilliseconds = pollingIntervalMilliseconds;
                }

                if (int.TryParse(section["RetryDelaySeconds"], out var retryDelaySeconds))
                {
                    options.RetryDelaySeconds = retryDelaySeconds;
                }
            })
            .Validate(options => options.MaxAttempts > 0, "MaxAttempts must be positive.")
            .Validate(options => options.PollingIntervalMilliseconds > 0, "PollingIntervalMilliseconds must be positive.")
            .Validate(options => options.RetryDelaySeconds > 0, "RetryDelaySeconds must be positive.")
            .ValidateOnStart();

        services.AddScoped<IWorkerPipelineService, WorkerPipelineService>();

        return services;
    }

    public static IEndpointRouteBuilder MapWorkerPipelineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/worker-pipeline/jobs/analyze-uploaded-video",
            async Task<IResult> (
                AnalyzeUploadedVideoJobRequestV1Dto request,
                IWorkerPipelineService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.EnqueueAnalyzeUploadedVideoAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Worker pipeline rejected job enqueue");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_worker_pipeline_job_request",
                        message = exception.Message
                    });
                }
            });

        endpoints.MapPost(
            "/api/worker-pipeline/jobs/process-one",
            async Task<IResult> (
                IWorkerPipelineService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.ProcessOneAsync(cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapGet(
            "/api/worker-pipeline/jobs/{jobId:guid}",
            async Task<IResult> (
                Guid jobId,
                IWorkerPipelineService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAsync(jobId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

        endpoints.MapGet(
            "/api/worker-pipeline/jobs",
            async Task<IResult> (
                int? take,
                IWorkerPipelineService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.ListRecentAsync(take ?? 25, cancellationToken);
                return Results.Ok(result);
            });

        return endpoints;
    }
}