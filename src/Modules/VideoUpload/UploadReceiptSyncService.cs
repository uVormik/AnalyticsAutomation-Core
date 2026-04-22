using System.Security.Claims;
using System.Text.Json;

using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Infrastructure.Observability;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.VideoUpload;

public interface IUploadReceiptSyncService
{
    Task<VideoUploadReceiptResponseDto> AcceptAsync(
        VideoUploadReceiptSyncRequestDto request,
        AuthenticatedVideoUploadScope authenticatedScope,
        CancellationToken cancellationToken);
}

public sealed record AuthenticatedVideoUploadScope(
    Guid UserId,
    Guid? DeviceId,
    Guid? GroupNodeId)
{
    private const string DeviceIdClaimType = "device_id";
    private const string GroupNodeIdClaimType = "current_group_node_id";

    public static AuthenticatedVideoUploadScope FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userId = ParseRequiredGuidClaim(principal, ClaimTypes.NameIdentifier, "Authenticated user id claim is required.");
        var deviceId = ParseOptionalGuidClaim(principal, DeviceIdClaimType);
        var groupNodeId = ParseOptionalGuidClaim(principal, GroupNodeIdClaimType);

        return new AuthenticatedVideoUploadScope(userId, deviceId, groupNodeId);
    }

    private static Guid ParseRequiredGuidClaim(
        ClaimsPrincipal principal,
        string claimType,
        string errorMessage)
    {
        var value = principal.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            throw new InvalidOperationException(errorMessage);
        }

        return parsed;
    }

    private static Guid? ParseOptionalGuidClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            throw new InvalidOperationException($"Authenticated {claimType} claim is invalid.");
        }

        return parsed;
    }
}

internal sealed record UploadReceiptPersistRequest(
    Guid PreUploadCheckId,
    Guid UserId,
    Guid? DeviceId,
    Guid? GroupNodeId,
    string ExternalVideoId,
    string StorageKey,
    string SiteStatus,
    long SizeBytes,
    string ByteSha256,
    string IdempotencyKey,
    DateTimeOffset UploadedAtUtc,
    string AuditAction,
    string AcceptedMessage);

internal sealed record UploadReceiptPersistResult(
    VideoUploadReceiptResponseDto Response,
    Guid UploadReceiptId,
    Guid PreUploadCheckId,
    Guid AnalysisJobId);

internal static class VideoUploadRequestSupport
{
    private const string DeepAnalysisCommandName = "video-upload.deep-analysis";
    private const string ReceiptAuditCategory = "video_upload";
    private const string RequestParamName = "request";

    public static void ValidatePreUploadRequest(
        Guid userId,
        string businessObjectKey,
        string fileName,
        long sizeBytes,
        string byteSha256,
        VideoUploadOptions options)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", RequestParamName);
        }

        if (string.IsNullOrWhiteSpace(businessObjectKey))
        {
            throw new ArgumentException("BusinessObjectKey is required.", RequestParamName);
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName is required.", RequestParamName);
        }

        ValidateSizeBytes(sizeBytes, options.MaxFastAllowSizeBytes, RequestParamName);
        ValidateByteSha256(byteSha256, RequestParamName);
    }

    public static void ValidateUploadReceiptRequest(VideoUploadReceiptRequestDto request)
    {
        if (request.PreUploadCheckId == Guid.Empty)
        {
            throw new ArgumentException("PreUploadCheckId is required.", nameof(request));
        }

        ValidateUploadReceiptFields(
            request.UserId,
            request.ExternalVideoId,
            request.StorageKey,
            request.SiteStatus,
            request.SizeBytes,
            request.ByteSha256,
            request.IdempotencyKey);
    }

    public static void ValidateUploadReceiptSyncRequest(
        VideoUploadReceiptSyncRequestDto request,
        VideoUploadOptions options)
    {
        ValidatePreUploadRequest(
            request.UserId,
            request.BusinessObjectKey,
            request.FileName,
            request.SizeBytes,
            request.ByteSha256,
            options);

        ValidateUploadReceiptFields(
            request.UserId,
            request.ExternalVideoId,
            request.StorageKey,
            request.SiteStatus,
            request.SizeBytes,
            request.ByteSha256,
            request.IdempotencyKey);
    }

    public static string NormalizeHash(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    public static VideoUploadReceiptResponseDto CreateAlreadyAcceptedResponse(
        VideoUploadReceipt existing,
        string message)
    {
        return new VideoUploadReceiptResponseDto(
            UploadReceiptId: existing.Id,
            PreUploadCheckId: existing.PreUploadCheckId,
            Status: VideoUploadReceiptStatuses.AlreadyAccepted,
            Accepted: true,
            WasAlreadyAccepted: true,
            Message: message,
            AnalysisJobStatus: existing.AnalysisJobStatus,
            ReceivedAtUtc: existing.ReceivedAtUtc);
    }

    public static async Task<UploadReceiptPersistResult> PersistAcceptedReceiptAsync(
        PlatformDbContext dbContext,
        UploadReceiptPersistRequest request,
        VideoUploadPreUploadCheck? preUploadCheckToCreate,
        CancellationToken cancellationToken)
    {
        if (preUploadCheckToCreate is not null && preUploadCheckToCreate.Id != request.PreUploadCheckId)
        {
            throw new InvalidOperationException("Upload receipt persistence pre-upload check did not match the requested id.");
        }

        var receivedAtUtc = DateTimeOffset.UtcNow;
        var receiptId = Guid.NewGuid();
        var analysisJobId = Guid.NewGuid();
        var correlationId = $"upload-receipt-{receiptId:N}";

        var receipt = new VideoUploadReceipt
        {
            Id = receiptId,
            PreUploadCheckId = request.PreUploadCheckId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            GroupNodeId = request.GroupNodeId,
            ExternalVideoId = request.ExternalVideoId,
            StorageKey = request.StorageKey,
            SiteStatus = request.SiteStatus,
            SizeBytes = request.SizeBytes,
            ByteSha256 = request.ByteSha256,
            IdempotencyKey = request.IdempotencyKey,
            ReceiptStatus = VideoUploadReceiptStatuses.Accepted,
            AnalysisJobStatus = "queued",
            UploadedAtUtc = request.UploadedAtUtc,
            ReceivedAtUtc = receivedAtUtc
        };

        var job = new VideoUploadReceiptAnalysisJob
        {
            Id = analysisJobId,
            UploadReceiptId = receiptId,
            PreUploadCheckId = request.PreUploadCheckId,
            CommandName = DeepAnalysisCommandName,
            Status = "queued",
            EnqueuedAtUtc = receivedAtUtc
        };

        var auditPayload = JsonSerializer.Serialize(new
        {
            receipt.Id,
            receipt.PreUploadCheckId,
            receipt.UserId,
            receipt.DeviceId,
            receipt.GroupNodeId,
            receipt.ExternalVideoId,
            receipt.StorageKey,
            receipt.ByteSha256,
            receipt.SizeBytes
        });

        var audit = new VideoUploadReceiptAuditRecord
        {
            Id = Guid.NewGuid(),
            UploadReceiptId = receiptId,
            Category = ReceiptAuditCategory,
            Action = request.AuditAction,
            CorrelationId = correlationId,
            PayloadJson = auditPayload,
            CreatedAtUtc = receivedAtUtc
        };

        if (preUploadCheckToCreate is not null)
        {
            dbContext.VideoUploadPreUploadChecks.Add(preUploadCheckToCreate);
        }

        dbContext.VideoUploadReceipts.Add(receipt);
        dbContext.VideoUploadReceiptAnalysisJobs.Add(job);
        dbContext.VideoUploadReceiptAuditRecords.Add(audit);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UploadReceiptPersistResult(
            Response: new VideoUploadReceiptResponseDto(
                UploadReceiptId: receipt.Id,
                PreUploadCheckId: receipt.PreUploadCheckId,
                Status: VideoUploadReceiptStatuses.Accepted,
                Accepted: true,
                WasAlreadyAccepted: false,
                Message: request.AcceptedMessage,
                AnalysisJobStatus: receipt.AnalysisJobStatus,
                ReceivedAtUtc: receipt.ReceivedAtUtc),
            UploadReceiptId: receipt.Id,
            PreUploadCheckId: receipt.PreUploadCheckId,
            AnalysisJobId: analysisJobId);
    }

    private static void ValidateUploadReceiptFields(
        Guid userId,
        string externalVideoId,
        string storageKey,
        string siteStatus,
        long sizeBytes,
        string byteSha256,
        string idempotencyKey)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", RequestParamName);
        }

        if (string.IsNullOrWhiteSpace(externalVideoId))
        {
            throw new ArgumentException("ExternalVideoId is required.", RequestParamName);
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey is required.", RequestParamName);
        }

        if (string.IsNullOrWhiteSpace(siteStatus))
        {
            throw new ArgumentException("SiteStatus is required.", RequestParamName);
        }

        ValidateSizeBytes(sizeBytes, long.MaxValue, RequestParamName);
        ValidateByteSha256(byteSha256, RequestParamName);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey is required.", RequestParamName);
        }
    }

    private static void ValidateSizeBytes(long sizeBytes, long maxSizeBytes, string paramName)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentException("SizeBytes must be positive.", paramName);
        }

        if (sizeBytes > maxSizeBytes)
        {
            throw new ArgumentException("SizeBytes exceeds the configured PreUploadCheck limit.", paramName);
        }
    }

    private static void ValidateByteSha256(string byteSha256, string paramName)
    {
        if (string.IsNullOrWhiteSpace(byteSha256))
        {
            throw new ArgumentException("ByteSha256 is required.", paramName);
        }

        var normalizedHash = byteSha256.Trim();
        if (normalizedHash.Length != 64 || !normalizedHash.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("ByteSha256 must be a 64-character hexadecimal SHA-256 value.", paramName);
        }
    }
}

public sealed class UploadReceiptSyncService(
    PlatformDbContext dbContext,
    IOptions<VideoUploadOptions> options,
    IAuditService auditService,
    ILogger<UploadReceiptSyncService> logger) : IUploadReceiptSyncService
{
    private const string SyncReasonCode = "late_sync_reconciled";
    private const string SyncReasonCodeNeedsReview = "late_sync_context_incomplete";

    public async Task<VideoUploadReceiptResponseDto> AcceptAsync(
        VideoUploadReceiptSyncRequestDto request,
        AuthenticatedVideoUploadScope authenticatedScope,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = options.Value;
            if (!value.UploadReceiptSyncEnabled)
            {
                throw new InvalidOperationException("UploadReceiptSync is disabled.");
            }

            VideoUploadRequestSupport.ValidateUploadReceiptSyncRequest(request, value);
            ValidateScope(request, authenticatedScope);

            var normalizedHash = VideoUploadRequestSupport.NormalizeHash(request.ByteSha256);
            var normalizedBusinessObjectKey = VideoUploadRequestSupport.NormalizeRequired(request.BusinessObjectKey);
            var normalizedFileName = VideoUploadRequestSupport.NormalizeRequired(request.FileName);
            var normalizedContentType = VideoUploadRequestSupport.NormalizeOptional(request.ContentType);
            var normalizedExternalVideoId = VideoUploadRequestSupport.NormalizeRequired(request.ExternalVideoId);
            var normalizedStorageKey = VideoUploadRequestSupport.NormalizeRequired(request.StorageKey);
            var normalizedSiteStatus = VideoUploadRequestSupport.NormalizeRequired(request.SiteStatus).ToLowerInvariant();
            var normalizedIdempotencyKey = VideoUploadRequestSupport.NormalizeRequired(request.IdempotencyKey);

            var existingByIdempotency = await dbContext.VideoUploadReceipts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.IdempotencyKey == normalizedIdempotencyKey,
                    cancellationToken);

            if (existingByIdempotency is not null)
            {
                logger.LogInformation(
                    "UploadReceiptSync idempotency hit. UploadReceiptId={UploadReceiptId} PreUploadCheckId={PreUploadCheckId}",
                    existingByIdempotency.Id,
                    existingByIdempotency.PreUploadCheckId);

                var response = VideoUploadRequestSupport.CreateAlreadyAcceptedResponse(
                    existingByIdempotency,
                    "Upload receipt sync already accepted.");

                await TryWriteAuditAsync(
                    "upload_receipt_sync_already_accepted",
                    request,
                    authenticatedScope,
                    existingByIdempotency.Id,
                    new
                    {
                        existingByIdempotency.Id,
                        existingByIdempotency.PreUploadCheckId,
                        existingByIdempotency.IdempotencyKey,
                        MatchKind = "idempotency_key"
                    },
                    cancellationToken);

                return response;
            }

            var existingByNaturalKey = await dbContext.VideoUploadReceipts
                .AsNoTracking()
                .Where(item =>
                    item.UserId == authenticatedScope.UserId &&
                    item.DeviceId == request.DeviceId &&
                    item.GroupNodeId == request.GroupNodeId &&
                    item.ExternalVideoId == normalizedExternalVideoId &&
                    item.StorageKey == normalizedStorageKey &&
                    item.ByteSha256 == normalizedHash &&
                    item.SizeBytes == request.SizeBytes)
                .OrderBy(item => item.ReceivedAtUtc)
                .ThenBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingByNaturalKey is not null)
            {
                logger.LogInformation(
                    "UploadReceiptSync natural-key idempotency hit. UploadReceiptId={UploadReceiptId} PreUploadCheckId={PreUploadCheckId}",
                    existingByNaturalKey.Id,
                    existingByNaturalKey.PreUploadCheckId);

                var response = VideoUploadRequestSupport.CreateAlreadyAcceptedResponse(
                    existingByNaturalKey,
                    "Upload receipt sync already accepted.");

                await TryWriteAuditAsync(
                    "upload_receipt_sync_already_accepted",
                    request,
                    authenticatedScope,
                    existingByNaturalKey.Id,
                    new
                    {
                        existingByNaturalKey.Id,
                        existingByNaturalKey.PreUploadCheckId,
                        existingByNaturalKey.ExternalVideoId,
                        existingByNaturalKey.StorageKey,
                        MatchKind = "natural_receipt_key"
                    },
                    cancellationToken);

                return response;
            }

            var existingPreUploadCheck = await dbContext.VideoUploadPreUploadChecks
                .AsNoTracking()
                .Where(item =>
                    item.CanUploadToSite &&
                    item.UserId == authenticatedScope.UserId &&
                    item.DeviceId == request.DeviceId &&
                    item.GroupNodeId == request.GroupNodeId &&
                    item.BusinessObjectKey == normalizedBusinessObjectKey &&
                    item.ExternalVideoId == normalizedExternalVideoId &&
                    item.StorageKey == normalizedStorageKey &&
                    item.ByteSha256 == normalizedHash &&
                    item.SizeBytes == request.SizeBytes)
                .OrderBy(item => item.CheckedAtUtc)
                .ThenBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var preUploadCheckToCreate = existingPreUploadCheck is null
                ? await CreateLateSyncPreUploadCheckAsync(
                    request,
                    authenticatedScope,
                    normalizedBusinessObjectKey,
                    normalizedFileName,
                    normalizedContentType,
                    normalizedExternalVideoId,
                    normalizedStorageKey,
                    normalizedHash,
                    value.SiteProvider,
                    cancellationToken)
                : null;

            var persisted = await VideoUploadRequestSupport.PersistAcceptedReceiptAsync(
                dbContext,
                new UploadReceiptPersistRequest(
                    PreUploadCheckId: existingPreUploadCheck?.Id ?? preUploadCheckToCreate!.Id,
                    UserId: authenticatedScope.UserId,
                    DeviceId: request.DeviceId,
                    GroupNodeId: request.GroupNodeId,
                    ExternalVideoId: normalizedExternalVideoId,
                    StorageKey: normalizedStorageKey,
                    SiteStatus: normalizedSiteStatus,
                    SizeBytes: request.SizeBytes,
                    ByteSha256: normalizedHash,
                    IdempotencyKey: normalizedIdempotencyKey,
                    UploadedAtUtc: request.UploadedAtUtc,
                    AuditAction: "upload_receipt_sync_accepted",
                    AcceptedMessage: "Upload receipt sync accepted and analysis job queued."),
                preUploadCheckToCreate,
                cancellationToken);

            logger.LogInformation(
                "UploadReceiptSync accepted. UploadReceiptId={UploadReceiptId} PreUploadCheckId={PreUploadCheckId} ExternalVideoId={ExternalVideoId}",
                persisted.UploadReceiptId,
                persisted.PreUploadCheckId,
                normalizedExternalVideoId);

            await TryWriteAuditAsync(
                "upload_receipt_sync_accepted",
                request,
                authenticatedScope,
                persisted.UploadReceiptId,
                new
                {
                    persisted.UploadReceiptId,
                    persisted.PreUploadCheckId,
                    ReusedPreUploadCheck = existingPreUploadCheck is not null
                },
                cancellationToken);

            await TryWriteAuditAsync(
                "upload_receipt_sync_analysis_job_queued",
                request,
                authenticatedScope,
                persisted.UploadReceiptId,
                new
                {
                    persisted.UploadReceiptId,
                    persisted.PreUploadCheckId,
                    persisted.AnalysisJobId
                },
                cancellationToken);

            return persisted.Response;
        }
        catch (ArgumentException exception)
        {
            await TryWriteAuditAsync(
                "upload_receipt_sync_rejected",
                request,
                authenticatedScope,
                null,
                new
                {
                    Error = exception.Message,
                    RejectionKind = "invalid_request"
                },
                cancellationToken);

            throw;
        }
        catch (InvalidOperationException exception)
        {
            await TryWriteAuditAsync(
                "upload_receipt_sync_rejected",
                request,
                authenticatedScope,
                null,
                new
                {
                    Error = exception.Message,
                    RejectionKind = "conflict"
                },
                cancellationToken);

            throw;
        }
    }

    private async Task<VideoUploadPreUploadCheck> CreateLateSyncPreUploadCheckAsync(
        VideoUploadReceiptSyncRequestDto request,
        AuthenticatedVideoUploadScope authenticatedScope,
        string normalizedBusinessObjectKey,
        string normalizedFileName,
        string? normalizedContentType,
        string normalizedExternalVideoId,
        string normalizedStorageKey,
        string normalizedHash,
        string siteProvider,
        CancellationToken cancellationToken)
    {
        var relatedPreUploadCheckId = await dbContext.VideoUploadPreUploadChecks
            .AsNoTracking()
            .Where(item =>
                item.UserId == authenticatedScope.UserId &&
                item.ByteSha256 == normalizedHash &&
                item.SizeBytes == request.SizeBytes)
            .OrderBy(item => item.CheckedAtUtc)
            .ThenBy(item => item.Id)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var allowWithReview = request.DeviceId is null || request.GroupNodeId is null;

        return new VideoUploadPreUploadCheck
        {
            Id = Guid.NewGuid(),
            UserId = authenticatedScope.UserId,
            DeviceId = request.DeviceId,
            GroupNodeId = request.GroupNodeId,
            BusinessObjectKey = normalizedBusinessObjectKey,
            FileName = normalizedFileName,
            SizeBytes = request.SizeBytes,
            ByteSha256 = normalizedHash,
            ContentType = normalizedContentType,
            CapturedAtUtc = request.CapturedAtUtc,
            Decision = allowWithReview
                ? PreUploadCheckDecisions.AllowWithReview
                : PreUploadCheckDecisions.Allow,
            CanUploadToSite = true,
            ReasonCode = allowWithReview
                ? SyncReasonCodeNeedsReview
                : SyncReasonCode,
            ExistingPreUploadCheckId = relatedPreUploadCheckId,
            RequiredNextSteps = "RECEIVE_UPLOAD_RECEIPT_SYNC,QUEUE_ANALYSIS",
            SiteProvider = siteProvider,
            ExternalVideoId = normalizedExternalVideoId,
            StorageKey = normalizedStorageKey,
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateScope(
        VideoUploadReceiptSyncRequestDto request,
        AuthenticatedVideoUploadScope authenticatedScope)
    {
        if (request.UserId != authenticatedScope.UserId)
        {
            throw new InvalidOperationException("UploadReceiptSync user context does not match bearer token.");
        }

        if (authenticatedScope.DeviceId.HasValue && request.DeviceId != authenticatedScope.DeviceId)
        {
            throw new InvalidOperationException("UploadReceiptSync device context does not match bearer token.");
        }

        if (authenticatedScope.GroupNodeId.HasValue && request.GroupNodeId != authenticatedScope.GroupNodeId)
        {
            throw new InvalidOperationException("UploadReceiptSync group context does not match bearer token.");
        }
    }

    private async Task TryWriteAuditAsync(
        string action,
        VideoUploadReceiptSyncRequestDto request,
        AuthenticatedVideoUploadScope authenticatedScope,
        Guid? uploadReceiptId,
        object extraPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditService.WriteAsync(
                new AuditWriteEntry(
                    AuditCategories.VideoUpload,
                    action,
                    authenticatedScope.UserId,
                    request.DeviceId ?? authenticatedScope.DeviceId,
                    "video_upload_receipt_sync",
                    uploadReceiptId?.ToString("D") ?? VideoUploadRequestSupport.NormalizeRequired(request.IdempotencyKey),
                    new
                    {
                        request.UserId,
                        request.DeviceId,
                        request.GroupNodeId,
                        request.BusinessObjectKey,
                        request.FileName,
                        request.ExternalVideoId,
                        request.StorageKey,
                        request.SiteStatus,
                        request.SizeBytes,
                        request.ByteSha256,
                        request.IdempotencyKey,
                        request.CapturedAtUtc,
                        request.UploadedAtUtc,
                        AuthenticatedUserId = authenticatedScope.UserId,
                        AuthenticatedDeviceId = authenticatedScope.DeviceId,
                        AuthenticatedGroupNodeId = authenticatedScope.GroupNodeId,
                        Extra = extraPayload
                    }),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "UploadReceiptSync audit write failed. Action={Action} IdempotencyKey={IdempotencyKey}",
                action,
                request.IdempotencyKey);
        }
    }
}