using System.Globalization;
using System.Text.Json;

using BuildingBlocks.Contracts.VideoUpload;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.VideoUpload;

public sealed class VideoUploadOptions
{
    public const string SectionName = "Modules:VideoUpload";

    public bool PreUploadCheckEnabled { get; set; } = true;
    public bool UploadReceiptEnabled { get; set; } = true;
    public bool UploadReceiptSyncEnabled { get; set; } = true;
    public long MaxFastAllowSizeBytes { get; set; } = 5L * 1024L * 1024L * 1024L;
    public string SiteProvider { get; set; } = "Stub";
    public string ExternalVideoIdPrefix { get; set; } = "site-video";
    public string StorageKeyPrefix { get; set; } = "videos";
}

public interface IPreUploadCheckService
{
    Task<VideoPreUploadCheckResponseDto> CheckAsync(
        VideoPreUploadCheckRequestDto request,
        CancellationToken cancellationToken);
}

public interface IUploadReceiptService
{
    Task<VideoUploadReceiptResponseDto> AcceptAsync(
        VideoUploadReceiptRequestDto request,
        CancellationToken cancellationToken);
}

public sealed class PreUploadCheckService(
    PlatformDbContext dbContext,
    IOptions<VideoUploadOptions> options,
    ILogger<PreUploadCheckService> logger) : IPreUploadCheckService
{
    public async Task<VideoPreUploadCheckResponseDto> CheckAsync(
        VideoPreUploadCheckRequestDto request,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        if (!value.PreUploadCheckEnabled)
        {
            throw new InvalidOperationException("PreUploadCheck is disabled.");
        }

        ValidateRequest(request, value);

        var normalizedHash = request.ByteSha256.Trim().ToLowerInvariant();
        var normalizedBusinessObjectKey = request.BusinessObjectKey.Trim();
        var normalizedFileName = request.FileName.Trim();
        var normalizedContentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? null
            : request.ContentType.Trim();

        var existingAllowed = await dbContext.VideoUploadPreUploadChecks
            .AsNoTracking()
            .Where(item =>
                item.ByteSha256 == normalizedHash &&
                item.SizeBytes == request.SizeBytes &&
                item.Decision == PreUploadCheckDecisions.Allow)
            .OrderBy(item => item.CheckedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var priorBlockedBySameUser = await dbContext.VideoUploadPreUploadChecks
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.UserId == request.UserId &&
                    item.ByteSha256 == normalizedHash &&
                    item.SizeBytes == request.SizeBytes &&
                    item.Decision == PreUploadCheckDecisions.BlockHardDuplicate,
                cancellationToken);

        var checkedAtUtc = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        string decision;
        string reasonCode;
        string message;
        Guid? existingPreUploadCheckId = null;

        if (priorBlockedBySameUser)
        {
            decision = PreUploadCheckDecisions.BlockPossibleFalsification;
            reasonCode = "repeated_block_after_hard_duplicate";
            message = "Repeated upload attempt after a hard duplicate block.";
            existingPreUploadCheckId = existingAllowed?.Id;
        }
        else if (existingAllowed is not null)
        {
            decision = PreUploadCheckDecisions.BlockHardDuplicate;
            reasonCode = "exact_fingerprint_match";
            message = "Exact byte fingerprint already exists in pre-upload registry.";
            existingPreUploadCheckId = existingAllowed.Id;
        }
        else if (request.DeviceId is null || request.GroupNodeId is null)
        {
            decision = PreUploadCheckDecisions.AllowWithReview;
            reasonCode = "context_incomplete";
            message = "Upload can continue, but missing device or group context requires review.";
        }
        else
        {
            decision = PreUploadCheckDecisions.Allow;
            reasonCode = "fast_path_clear";
            message = "No exact duplicate was found in the fast-path pre-upload registry.";
        }

        var canUploadToSite =
            decision is PreUploadCheckDecisions.Allow or PreUploadCheckDecisions.AllowWithReview;

        var externalVideoId = canUploadToSite
            ? $"{value.ExternalVideoIdPrefix}-{id:N}"
            : null;

        var storageKey = canUploadToSite
            ? $"{value.StorageKeyPrefix}/{externalVideoId}.mp4"
            : null;

        var requiredNextSteps = canUploadToSite
            ? new[] { "UPLOAD_TO_SITE_DIRECT", "SEND_UPLOAD_RECEIPT" }
            : new[] { "DO_NOT_UPLOAD", "SHOW_DECISION_TO_USER" };

        var entity = new VideoUploadPreUploadCheck
        {
            Id = id,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            GroupNodeId = request.GroupNodeId,
            BusinessObjectKey = normalizedBusinessObjectKey,
            FileName = normalizedFileName,
            SizeBytes = request.SizeBytes,
            ByteSha256 = normalizedHash,
            ContentType = normalizedContentType,
            CapturedAtUtc = request.CapturedAtUtc,
            Decision = decision,
            CanUploadToSite = canUploadToSite,
            ReasonCode = reasonCode,
            ExistingPreUploadCheckId = existingPreUploadCheckId,
            RequiredNextSteps = string.Join(',', requiredNextSteps),
            SiteProvider = canUploadToSite ? value.SiteProvider : null,
            ExternalVideoId = externalVideoId,
            StorageKey = storageKey,
            CheckedAtUtc = checkedAtUtc
        };

        dbContext.VideoUploadPreUploadChecks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "PreUploadCheck completed. Decision={Decision} UserId={UserId} GroupNodeId={GroupNodeId} SizeBytes={SizeBytes}",
            decision,
            request.UserId,
            request.GroupNodeId,
            request.SizeBytes);

        var sitePlan = canUploadToSite
            ? new VideoPreUploadSitePlanDto(
                Provider: value.SiteProvider,
                ExternalVideoId: externalVideoId!,
                StorageKey: storageKey!,
                RequiredReceiptEndpoint: "/api/video/upload-receipt")
            : null;

        return new VideoPreUploadCheckResponseDto(
            PreUploadCheckId: id,
            Decision: decision,
            CanUploadToSite: canUploadToSite,
            ReasonCode: reasonCode,
            Message: message,
            ExistingPreUploadCheckId: existingPreUploadCheckId?.ToString("D"),
            RequiredNextSteps: requiredNextSteps,
            SitePlan: sitePlan,
            CheckedAtUtc: checkedAtUtc);
    }

    private static void ValidateRequest(VideoPreUploadCheckRequestDto request, VideoUploadOptions options)
    {
        VideoUploadRequestSupport.ValidatePreUploadRequest(
            request.UserId,
            request.BusinessObjectKey,
            request.FileName,
            request.SizeBytes,
            request.ByteSha256,
            options);
    }
}

public sealed class UploadReceiptService(
    PlatformDbContext dbContext,
    IOptions<VideoUploadOptions> options,
    ILogger<UploadReceiptService> logger) : IUploadReceiptService
{
    public async Task<VideoUploadReceiptResponseDto> AcceptAsync(
        VideoUploadReceiptRequestDto request,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        if (!value.UploadReceiptEnabled)
        {
            throw new InvalidOperationException("UploadReceipt is disabled.");
        }

        VideoUploadRequestSupport.ValidateUploadReceiptRequest(request);

        var normalizedHash = VideoUploadRequestSupport.NormalizeHash(request.ByteSha256);
        var normalizedIdempotencyKey = VideoUploadRequestSupport.NormalizeRequired(request.IdempotencyKey);
        var normalizedExternalVideoId = VideoUploadRequestSupport.NormalizeRequired(request.ExternalVideoId);
        var normalizedStorageKey = VideoUploadRequestSupport.NormalizeRequired(request.StorageKey);
        var normalizedSiteStatus = VideoUploadRequestSupport.NormalizeRequired(request.SiteStatus).ToLowerInvariant();

        var existing = await dbContext.VideoUploadReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.IdempotencyKey == normalizedIdempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "UploadReceipt idempotency hit. UploadReceiptId={UploadReceiptId} PreUploadCheckId={PreUploadCheckId}",
                existing.Id,
                existing.PreUploadCheckId);

            return VideoUploadRequestSupport.CreateAlreadyAcceptedResponse(
                existing,
                "Upload receipt already accepted.");
        }

        var preUploadCheck = await dbContext.VideoUploadPreUploadChecks
            .SingleOrDefaultAsync(
                item => item.Id == request.PreUploadCheckId,
                cancellationToken);

        if (preUploadCheck is null)
        {
            throw new InvalidOperationException("PreUploadCheck was not found.");
        }

        if (!preUploadCheck.CanUploadToSite)
        {
            throw new InvalidOperationException("PreUploadCheck decision does not allow site upload.");
        }

        if (preUploadCheck.UserId != request.UserId)
        {
            throw new InvalidOperationException("UploadReceipt user context does not match PreUploadCheck.");
        }

        if (preUploadCheck.DeviceId != request.DeviceId || preUploadCheck.GroupNodeId != request.GroupNodeId)
        {
            throw new InvalidOperationException("UploadReceipt device or group context does not match PreUploadCheck.");
        }

        if (!string.Equals(preUploadCheck.ByteSha256, normalizedHash, StringComparison.OrdinalIgnoreCase) ||
            preUploadCheck.SizeBytes != request.SizeBytes)
        {
            throw new InvalidOperationException("UploadReceipt fingerprint does not match PreUploadCheck.");
        }

        if (!string.IsNullOrWhiteSpace(preUploadCheck.ExternalVideoId) &&
            !string.Equals(preUploadCheck.ExternalVideoId, normalizedExternalVideoId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("UploadReceipt external video id does not match PreUploadCheck.");
        }

        if (!string.IsNullOrWhiteSpace(preUploadCheck.StorageKey) &&
            !string.Equals(preUploadCheck.StorageKey, normalizedStorageKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("UploadReceipt storage key does not match PreUploadCheck.");
        }

        var persisted = await VideoUploadRequestSupport.PersistAcceptedReceiptAsync(
            dbContext,
            new UploadReceiptPersistRequest(
                PreUploadCheckId: request.PreUploadCheckId,
                UserId: request.UserId,
                DeviceId: request.DeviceId,
                GroupNodeId: request.GroupNodeId,
                ExternalVideoId: normalizedExternalVideoId,
                StorageKey: normalizedStorageKey,
                SiteStatus: normalizedSiteStatus,
                SizeBytes: request.SizeBytes,
                ByteSha256: normalizedHash,
                IdempotencyKey: normalizedIdempotencyKey,
                UploadedAtUtc: request.UploadedAtUtc,
                AuditAction: "upload_receipt_accepted",
                AcceptedMessage: "Upload receipt accepted and analysis job queued."),
            preUploadCheckToCreate: null,
            cancellationToken);

        logger.LogInformation(
            "UploadReceipt accepted. UploadReceiptId={UploadReceiptId} PreUploadCheckId={PreUploadCheckId} ExternalVideoId={ExternalVideoId}",
            persisted.UploadReceiptId,
            persisted.PreUploadCheckId,
            normalizedExternalVideoId);

        return persisted.Response;
    }
}

public static class VideoUploadModule
{
    public static IServiceCollection AddVideoUploadModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<VideoUploadOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(VideoUploadOptions.SectionName);

                if (bool.TryParse(section["PreUploadCheckEnabled"], out var preUploadCheckEnabled))
                {
                    options.PreUploadCheckEnabled = preUploadCheckEnabled;
                }

                if (bool.TryParse(section["UploadReceiptEnabled"], out var uploadReceiptEnabled))
                {
                    options.UploadReceiptEnabled = uploadReceiptEnabled;
                }

                if (bool.TryParse(section["UploadReceiptSyncEnabled"], out var uploadReceiptSyncEnabled))
                {
                    options.UploadReceiptSyncEnabled = uploadReceiptSyncEnabled;
                }

                if (long.TryParse(
                        section["MaxFastAllowSizeBytes"],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var maxFastAllowSizeBytes))
                {
                    options.MaxFastAllowSizeBytes = maxFastAllowSizeBytes;
                }

                options.SiteProvider = string.IsNullOrWhiteSpace(section["SiteProvider"])
                    ? options.SiteProvider
                    : section["SiteProvider"]!;

                options.ExternalVideoIdPrefix = string.IsNullOrWhiteSpace(section["ExternalVideoIdPrefix"])
                    ? options.ExternalVideoIdPrefix
                    : section["ExternalVideoIdPrefix"]!;

                options.StorageKeyPrefix = string.IsNullOrWhiteSpace(section["StorageKeyPrefix"])
                    ? options.StorageKeyPrefix
                    : section["StorageKeyPrefix"]!;
            })
            .Validate(
                options => options.MaxFastAllowSizeBytes > 0,
                "Modules:VideoUpload:MaxFastAllowSizeBytes must be positive.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SiteProvider),
                "Modules:VideoUpload:SiteProvider is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ExternalVideoIdPrefix),
                "Modules:VideoUpload:ExternalVideoIdPrefix is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.StorageKeyPrefix),
                "Modules:VideoUpload:StorageKeyPrefix is required.")
            .ValidateOnStart();

        services.AddScoped<IPreUploadCheckService, PreUploadCheckService>();
        services.AddScoped<IUploadReceiptService, UploadReceiptService>();
        services.AddScoped<IUploadReceiptSyncService, UploadReceiptSyncService>();

        return services;
    }

    public static IEndpointRouteBuilder MapVideoUploadEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/video/pre-upload-check",
            async Task<IResult> (
                VideoPreUploadCheckRequestDto request,
                IPreUploadCheckService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.CheckAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "PreUploadCheck unavailable");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_pre_upload_check_request",
                        message = exception.Message
                    });
                }
            })
            .RequireAuthorization();

        endpoints.MapPost(
            "/api/video/upload-receipt-sync",
            async Task<IResult> (
                VideoUploadReceiptSyncRequestDto request,
                HttpContext httpContext,
                IUploadReceiptSyncService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var authenticatedScope = AuthenticatedVideoUploadScope.FromClaimsPrincipal(httpContext.User);
                    var result = await service.AcceptAsync(request, authenticatedScope, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                    when (exception.Message.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "UploadReceiptSync unavailable");
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "UploadReceiptSync rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_upload_receipt_sync_request",
                        message = exception.Message
                    });
                }
            })
            .RequireAuthorization();

        endpoints.MapPost(
            "/api/video/upload-receipt",
            async Task<IResult> (
                VideoUploadReceiptRequestDto request,
                IUploadReceiptService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.AcceptAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "UploadReceipt rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_upload_receipt_request",
                        message = exception.Message
                    });
                }
            })
            .RequireAuthorization();

        return endpoints;
    }
}