using System.Text.Json;
using System.Text.RegularExpressions;

using BuildingBlocks.Contracts.Incidents;
using BuildingBlocks.Contracts.VideoDuplicates;
using BuildingBlocks.Contracts.WorkerPipeline;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Modules.GroupTree;
using Modules.Incidents;
using Modules.VideoDuplicates;

namespace Modules.WorkerPipeline;

public interface IUploadReceiptPipelineBridgeService
{
    Task<UploadReceiptPipelineProcessResult> ProcessNextQueuedAsync(CancellationToken cancellationToken);

    Task<UploadReceiptPipelineProcessResult> ProcessAsync(
        Guid analysisJobId,
        CancellationToken cancellationToken);
}

public sealed record UploadReceiptPipelineProcessResult(
    bool Processed,
    Guid? AnalysisJobId,
    Guid? UploadReceiptId,
    string? JobStatus,
    Guid? VideoAssetId,
    IReadOnlyCollection<Guid> DuplicateCandidateIds,
    IReadOnlyCollection<Guid> IncidentIds,
    string Message);

public sealed class UploadReceiptPipelineBridgeService(
    PlatformDbContext dbContext,
    IVideoDuplicateRegistryService duplicateRegistryService,
    IGroupTreeQueryService groupTreeQueryService,
    IDuplicateIncidentRoutingService duplicateIncidentRoutingService,
    IOptions<WorkerPipelineOptions> options,
    ILogger<UploadReceiptPipelineBridgeService> logger) : IUploadReceiptPipelineBridgeService
{
    private const string DeepAnalysisCommandName = "video-upload.deep-analysis";
    private const string AuditCategory = "video_upload_analysis_job";

    private static readonly Regex SecretAssignmentPattern = new(
        "(?i)(password|accessToken|refreshToken)\\s*[:=]\\s*([^,;\\s]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<UploadReceiptPipelineProcessResult> ProcessNextQueuedAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.DeepAnalysisEnabled)
        {
            return CreateNoOpResult("Upload receipt analysis bridge is disabled.");
        }

        var job = await dbContext.VideoUploadReceiptAnalysisJobs
            .Where(item =>
                item.Status == WorkerPipelineJobStatusesV1.Queued &&
                item.CommandName == DeepAnalysisCommandName)
            .OrderBy(item => item.EnqueuedAtUtc)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return CreateNoOpResult("No queued upload receipt analysis job is available.");
        }

        return await ProcessTrackedAsync(job, cancellationToken);
    }

    public async Task<UploadReceiptPipelineProcessResult> ProcessAsync(
        Guid analysisJobId,
        CancellationToken cancellationToken)
    {
        if (analysisJobId == Guid.Empty)
        {
            throw new ArgumentException("AnalysisJobId is required.", nameof(analysisJobId));
        }

        if (!options.Value.DeepAnalysisEnabled)
        {
            return new UploadReceiptPipelineProcessResult(
                Processed: false,
                AnalysisJobId: analysisJobId,
                UploadReceiptId: null,
                JobStatus: null,
                VideoAssetId: null,
                DuplicateCandidateIds: Array.Empty<Guid>(),
                IncidentIds: Array.Empty<Guid>(),
                Message: "Upload receipt analysis bridge is disabled.");
        }

        var job = await dbContext.VideoUploadReceiptAnalysisJobs
            .SingleOrDefaultAsync(item => item.Id == analysisJobId, cancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException("Upload receipt analysis job was not found.");
        }

        if (!string.Equals(job.CommandName, DeepAnalysisCommandName, StringComparison.Ordinal))
        {
            return new UploadReceiptPipelineProcessResult(
                Processed: false,
                AnalysisJobId: job.Id,
                UploadReceiptId: job.UploadReceiptId,
                JobStatus: job.Status,
                VideoAssetId: null,
                DuplicateCandidateIds: Array.Empty<Guid>(),
                IncidentIds: Array.Empty<Guid>(),
                Message: "Upload receipt analysis job command is not supported by the bridge.");
        }

        if (job.Status == WorkerPipelineJobStatusesV1.Completed)
        {
            logger.LogInformation(
                "Upload receipt analysis job skipped because it is already completed. AnalysisJobId={AnalysisJobId} UploadReceiptId={UploadReceiptId}",
                job.Id,
                job.UploadReceiptId);

            return new UploadReceiptPipelineProcessResult(
                Processed: false,
                AnalysisJobId: job.Id,
                UploadReceiptId: job.UploadReceiptId,
                JobStatus: job.Status,
                VideoAssetId: null,
                DuplicateCandidateIds: Array.Empty<Guid>(),
                IncidentIds: Array.Empty<Guid>(),
                Message: "Upload receipt analysis job is already completed.");
        }

        if (job.Status != WorkerPipelineJobStatusesV1.Queued)
        {
            return new UploadReceiptPipelineProcessResult(
                Processed: false,
                AnalysisJobId: job.Id,
                UploadReceiptId: job.UploadReceiptId,
                JobStatus: job.Status,
                VideoAssetId: null,
                DuplicateCandidateIds: Array.Empty<Guid>(),
                IncidentIds: Array.Empty<Guid>(),
                Message: "Upload receipt analysis job is not queued.");
        }

        return await ProcessTrackedAsync(job, cancellationToken);
    }

    private async Task<UploadReceiptPipelineProcessResult> ProcessTrackedAsync(
        VideoUploadReceiptAnalysisJob job,
        CancellationToken cancellationToken)
    {
        var receipt = await dbContext.VideoUploadReceipts
            .SingleOrDefaultAsync(item => item.Id == job.UploadReceiptId, cancellationToken);

        if (receipt is null)
        {
            return await FailJobAsync(
                job,
                receipt,
                "Upload receipt was not found for analysis job processing.",
                cancellationToken);
        }

        if (receipt.AnalysisJobStatus == WorkerPipelineJobStatusesV1.Completed)
        {
            if (job.Status != WorkerPipelineJobStatusesV1.Completed)
            {
                job.Status = WorkerPipelineJobStatusesV1.Completed;
                AddAudit(
                    receipt.Id,
                    "upload_receipt_analysis_job_reconciled_completed",
                    new
                    {
                        AnalysisJobId = job.Id,
                        receipt.Id,
                        job.Status
                    });

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Upload receipt analysis job skipped because receipt is already completed. AnalysisJobId={AnalysisJobId} UploadReceiptId={UploadReceiptId}",
                job.Id,
                receipt.Id);

            return new UploadReceiptPipelineProcessResult(
                Processed: false,
                AnalysisJobId: job.Id,
                UploadReceiptId: receipt.Id,
                JobStatus: job.Status,
                VideoAssetId: null,
                DuplicateCandidateIds: Array.Empty<Guid>(),
                IncidentIds: Array.Empty<Guid>(),
                Message: "Upload receipt analysis job is already completed.");
        }

        try
        {
            job.Status = WorkerPipelineJobStatusesV1.Running;

            AddAudit(
                receipt.Id,
                "upload_receipt_analysis_job_started",
                new
                {
                    AnalysisJobId = job.Id,
                    receipt.Id,
                    job.CommandName,
                    receipt.ExternalVideoId,
                    receipt.StorageKey
                });

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Upload receipt analysis job started. AnalysisJobId={AnalysisJobId} UploadReceiptId={UploadReceiptId} CommandName={CommandName}",
                job.Id,
                receipt.Id,
                job.CommandName);

            var preUploadCheck = await dbContext.VideoUploadPreUploadChecks
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == job.PreUploadCheckId, cancellationToken);

            if (preUploadCheck is null)
            {
                throw new InvalidOperationException("PreUploadCheck was not found for upload receipt analysis job.");
            }

            var duplicateRegistration = await duplicateRegistryService.RegisterAssetAsync(
                new DuplicateAssetRegistrationRequestDto(
                    UploadReceiptId: receipt.Id,
                    PreUploadCheckId: receipt.PreUploadCheckId,
                    UserId: receipt.UserId,
                    DeviceId: receipt.DeviceId,
                    GroupNodeId: receipt.GroupNodeId,
                    BusinessObjectKey: preUploadCheck.BusinessObjectKey,
                    ExternalVideoId: receipt.ExternalVideoId,
                    StorageKey: receipt.StorageKey,
                    SizeBytes: receipt.SizeBytes,
                    ByteSha256: receipt.ByteSha256,
                    UploadedAtUtc: receipt.UploadedAtUtc),
                cancellationToken);

            var candidateIds = duplicateRegistration.Candidates
                .Select(item => item.CandidateId)
                .ToArray();

            var incidentIds = await CreateDuplicateIncidentsAsync(
                receipt,
                duplicateRegistration.Candidates,
                cancellationToken);

            job.Status = WorkerPipelineJobStatusesV1.Completed;
            receipt.AnalysisJobStatus = WorkerPipelineJobStatusesV1.Completed;

            AddAudit(
                receipt.Id,
                "upload_receipt_analysis_job_completed",
                new
                {
                    AnalysisJobId = job.Id,
                    receipt.Id,
                    duplicateRegistration.VideoAssetId,
                    DuplicateCandidateIds = candidateIds,
                    IncidentIds = incidentIds
                });

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Upload receipt analysis job completed. AnalysisJobId={AnalysisJobId} UploadReceiptId={UploadReceiptId} VideoAssetId={VideoAssetId} DuplicateCandidateCount={DuplicateCandidateCount} IncidentCount={IncidentCount}",
                job.Id,
                receipt.Id,
                duplicateRegistration.VideoAssetId,
                candidateIds.Length,
                incidentIds.Count);

            return new UploadReceiptPipelineProcessResult(
                Processed: true,
                AnalysisJobId: job.Id,
                UploadReceiptId: receipt.Id,
                JobStatus: job.Status,
                VideoAssetId: duplicateRegistration.VideoAssetId,
                DuplicateCandidateIds: candidateIds,
                IncidentIds: incidentIds,
                Message: "Upload receipt analysis job completed.");
        }
        catch (Exception exception)
        {
            return await FailJobAsync(job, receipt, exception.Message, cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<Guid>> CreateDuplicateIncidentsAsync(
        VideoUploadReceipt receipt,
        IReadOnlyCollection<DuplicateCandidateDto> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0 || receipt.GroupNodeId is null)
        {
            return Array.Empty<Guid>();
        }

        var routing = await groupTreeQueryService.PreviewRoutingAsync(
            receipt.GroupNodeId.Value,
            receipt.UserId,
            cancellationToken);

        if (routing is null || routing.ResolvedAdminUserIds.Count == 0)
        {
            logger.LogInformation(
                "Upload receipt analysis job skipped incident routing because no admin routing was resolved. UploadReceiptId={UploadReceiptId} GroupNodeId={GroupNodeId}",
                receipt.Id,
                receipt.GroupNodeId);

            return Array.Empty<Guid>();
        }

        var branchAdminUserIds = routing.EscalatedHigher
            ? Array.Empty<Guid>()
            : routing.ResolvedAdminUserIds.ToArray();

        var higherAdminUserIds = routing.EscalatedHigher
            ? routing.ResolvedAdminUserIds.ToArray()
            : Array.Empty<Guid>();

        var incidentIds = new List<Guid>(candidates.Count);
        foreach (var candidate in candidates)
        {
            try
            {
                var incident = await duplicateIncidentRoutingService.CreateFromCandidateAsync(
                    new DuplicateIncidentCreateRequestV1Dto(
                        DuplicateCandidateId: candidate.CandidateId,
                        SourceVideoAssetId: candidate.SourceVideoAssetId,
                        MatchedVideoAssetId: candidate.MatchedVideoAssetId,
                        UploaderUserId: receipt.UserId,
                        UploaderGroupNodeId: receipt.GroupNodeId.Value,
                        IsUploaderBranchAdmin: routing.EscalatedHigher,
                        BranchAdminUserIds: branchAdminUserIds,
                        HigherAdminUserIds: higherAdminUserIds,
                        MatchKind: candidate.MatchKind,
                        ReasonCode: candidate.ReasonCode,
                        DetectedAtUtc: candidate.DetectedAtUtc),
                    cancellationToken);

                incidentIds.Add(incident.IncidentId);
            }
            catch (InvalidOperationException exception)
                when (exception.Message.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation(
                    "Upload receipt analysis job skipped duplicate incident routing because the incidents module is disabled. UploadReceiptId={UploadReceiptId}",
                    receipt.Id);

                return Array.Empty<Guid>();
            }
        }

        return incidentIds;
    }

    private async Task<UploadReceiptPipelineProcessResult> FailJobAsync(
        VideoUploadReceiptAnalysisJob job,
        VideoUploadReceipt? receipt,
        string error,
        CancellationToken cancellationToken)
    {
        var sanitizedError = SanitizeMessage(error);

        job.Status = WorkerPipelineJobStatusesV1.Failed;

        if (receipt is not null)
        {
            receipt.AnalysisJobStatus = WorkerPipelineJobStatusesV1.Failed;

            AddAudit(
                receipt.Id,
                "upload_receipt_analysis_job_failed",
                new
                {
                    AnalysisJobId = job.Id,
                    receipt.Id,
                    Error = sanitizedError
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogError(
            "Upload receipt analysis job failed. AnalysisJobId={AnalysisJobId} UploadReceiptId={UploadReceiptId} Error={Error}",
            job.Id,
            job.UploadReceiptId,
            sanitizedError);

        return new UploadReceiptPipelineProcessResult(
            Processed: true,
            AnalysisJobId: job.Id,
            UploadReceiptId: job.UploadReceiptId,
            JobStatus: job.Status,
            VideoAssetId: null,
            DuplicateCandidateIds: Array.Empty<Guid>(),
            IncidentIds: Array.Empty<Guid>(),
            Message: sanitizedError);
    }

    private void AddAudit(Guid uploadReceiptId, string action, object payload)
    {
        dbContext.VideoUploadReceiptAuditRecords.Add(new VideoUploadReceiptAuditRecord
        {
            Id = Guid.NewGuid(),
            UploadReceiptId = uploadReceiptId,
            Category = AuditCategory,
            Action = action,
            CorrelationId = CreateCorrelationId(uploadReceiptId),
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private static UploadReceiptPipelineProcessResult CreateNoOpResult(string message)
    {
        return new UploadReceiptPipelineProcessResult(
            Processed: false,
            AnalysisJobId: null,
            UploadReceiptId: null,
            JobStatus: null,
            VideoAssetId: null,
            DuplicateCandidateIds: Array.Empty<Guid>(),
            IncidentIds: Array.Empty<Guid>(),
            Message: message);
    }

    private static string CreateCorrelationId(Guid uploadReceiptId)
    {
        return $"upload-receipt-{uploadReceiptId:N}";
    }

    private static string SanitizeMessage(string? message)
    {
        var value = string.IsNullOrWhiteSpace(message)
            ? "Upload receipt analysis job failed."
            : message.Trim();

        return SecretAssignmentPattern.Replace(
            value,
            match => $"{match.Groups[1].Value}=[REDACTED]");
    }
}

public sealed class UploadReceiptPipelineBridgeHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerPipelineOptions> options,
    ILogger<UploadReceiptPipelineBridgeHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Upload receipt pipeline bridge hosted service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IUploadReceiptPipelineBridgeService>();
                await service.ProcessNextQueuedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Upload receipt pipeline bridge hosted service iteration failed.");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(Math.Max(250, options.Value.PollingIntervalMilliseconds)),
                stoppingToken);
        }
    }
}