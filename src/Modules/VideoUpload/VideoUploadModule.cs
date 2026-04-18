using System.Globalization;

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
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.BusinessObjectKey))
        {
            throw new ArgumentException("BusinessObjectKey is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("FileName is required.", nameof(request));
        }

        if (request.SizeBytes <= 0)
        {
            throw new ArgumentException("SizeBytes must be positive.", nameof(request));
        }

        if (request.SizeBytes > options.MaxFastAllowSizeBytes)
        {
            throw new ArgumentException("SizeBytes exceeds the configured PreUploadCheck limit.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ByteSha256))
        {
            throw new ArgumentException("ByteSha256 is required.", nameof(request));
        }

        var normalizedHash = request.ByteSha256.Trim();
        if (normalizedHash.Length != 64 || !normalizedHash.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("ByteSha256 must be a 64-character hexadecimal SHA-256 value.", nameof(request));
        }
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

                if (bool.TryParse(section["PreUploadCheckEnabled"], out var enabled))
                {
                    options.PreUploadCheckEnabled = enabled;
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
                        title: "PreUploadCheck disabled");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_pre_upload_check_request",
                        message = exception.Message
                    });
                }
            });

        return endpoints;
    }
}