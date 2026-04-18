using System.Text.Json;

using BuildingBlocks.Contracts.VideoDownload;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDownload;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.VideoDownload;

public sealed class VideoDownloadOptions
{
    public const string SectionName = "Modules:VideoDownload";

    public bool Enabled { get; set; } = true;
    public int DefaultIntentTtlSeconds { get; set; } = 900;
    public string SiteProvider { get; set; } = "Stub";
    public string StubDownloadBaseUrl { get; set; } = "https://stub-video-site.local/download";
}

public interface IVideoDownloadService
{
    Task<CreateDownloadIntentResponseV1Dto> CreateDownloadIntentAsync(
        CreateDownloadIntentRequestV1Dto request,
        CancellationToken cancellationToken);

    Task<DownloadReceiptResponseV1Dto> AcceptDownloadReceiptAsync(
        DownloadReceiptRequestV1Dto request,
        CancellationToken cancellationToken);

    Task<DownloadIntentSummaryV1Dto?> GetIntentAsync(
        Guid downloadIntentId,
        CancellationToken cancellationToken);

    Task<DownloadReceiptSummaryV1Dto?> GetReceiptAsync(
        Guid downloadReceiptId,
        CancellationToken cancellationToken);
}

public sealed class VideoDownloadService(
    PlatformDbContext dbContext,
    IOptions<VideoDownloadOptions> options,
    ILogger<VideoDownloadService> logger) : IVideoDownloadService
{
    public async Task<CreateDownloadIntentResponseV1Dto> CreateDownloadIntentAsync(
        CreateDownloadIntentRequestV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            throw new InvalidOperationException("VideoDownload module is disabled.");
        }

        ValidateIntentRequest(request);

        var now = DateTimeOffset.UtcNow;
        var ttlSeconds = request.ExpiresInSeconds.GetValueOrDefault(options.Value.DefaultIntentTtlSeconds);
        ttlSeconds = Math.Clamp(ttlSeconds, 60, 86400);

        var intentId = Guid.NewGuid();
        var externalVideoId = request.ExternalVideoId.Trim();
        var downloadUrl = BuildDownloadUrl(intentId, externalVideoId, options.Value);

        var intent = new VideoDownloadIntent
        {
            Id = intentId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            GroupNodeId = request.GroupNodeId,
            VideoAssetId = request.VideoAssetId,
            ExternalVideoId = externalVideoId,
            BusinessObjectKey = request.BusinessObjectKey.Trim(),
            Status = DownloadIntentV1Statuses.Created,
            SiteProvider = string.IsNullOrWhiteSpace(options.Value.SiteProvider)
                ? "Stub"
                : options.Value.SiteProvider.Trim(),
            DownloadUrl = downloadUrl,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddSeconds(ttlSeconds),
            ConsumedAtUtc = null,
            RejectedReason = null
        };

        dbContext.VideoDownloadIntents.Add(intent);
        AddAudit(
            downloadIntentId: intent.Id,
            downloadReceiptId: null,
            action: "download_intent_created",
            payload: new
            {
                IntentId = intent.Id,
                intent.UserId,
                intent.DeviceId,
                intent.GroupNodeId,
                intent.VideoAssetId,
                intent.ExternalVideoId,
                intent.BusinessObjectKey,
                intent.SiteProvider,
                intent.ExpiresAtUtc
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Download intent created. IntentId={IntentId} UserId={UserId} ExternalVideoId={ExternalVideoId}",
            intent.Id,
            intent.UserId,
            intent.ExternalVideoId);

        return ToIntentResponse(intent);
    }

    public async Task<DownloadReceiptResponseV1Dto> AcceptDownloadReceiptAsync(
        DownloadReceiptRequestV1Dto request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            throw new InvalidOperationException("VideoDownload module is disabled.");
        }

        ValidateReceiptRequest(request);

        var intent = await dbContext.VideoDownloadIntents
            .SingleOrDefaultAsync(
                item => item.Id == request.DownloadIntentId,
                cancellationToken);

        if (intent is null)
        {
            throw new InvalidOperationException("DownloadIntent was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (intent.Status == DownloadIntentV1Statuses.Expired ||
            intent.ExpiresAtUtc < now && intent.Status != DownloadIntentV1Statuses.Consumed)
        {
            intent.Status = DownloadIntentV1Statuses.Expired;
            intent.RejectedReason = "intent_expired";

            AddAudit(
                downloadIntentId: intent.Id,
                downloadReceiptId: null,
                action: "download_intent_expired",
                payload: new
                {
                    IntentId = intent.Id,
                    intent.ExpiresAtUtc,
                    NowUtc = now
                });

            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("DownloadIntent is expired.");
        }

        if (!string.Equals(intent.ExternalVideoId, request.ExternalVideoId.Trim(), StringComparison.Ordinal))
        {
            intent.Status = DownloadIntentV1Statuses.Rejected;
            intent.RejectedReason = "external_video_id_mismatch";

            AddAudit(
                downloadIntentId: intent.Id,
                downloadReceiptId: null,
                action: "download_intent_rejected",
                payload: new
                {
                    IntentId = intent.Id,
                    ExpectedExternalVideoId = intent.ExternalVideoId,
                    ActualExternalVideoId = request.ExternalVideoId.Trim()
                });

            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("DownloadReceipt does not match DownloadIntent external video id.");
        }

        var normalizedClientReceiptKey = string.IsNullOrWhiteSpace(request.ClientReceiptKey)
            ? null
            : request.ClientReceiptKey.Trim();

        var existingReceiptQuery = dbContext.VideoDownloadReceipts
            .AsNoTracking()
            .Where(item => item.DownloadIntentId == request.DownloadIntentId);

        if (normalizedClientReceiptKey is not null)
        {
            existingReceiptQuery = existingReceiptQuery
                .Where(item => item.ClientReceiptKey == normalizedClientReceiptKey);
        }

        var existingReceipt = await existingReceiptQuery
            .OrderBy(item => item.AcceptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingReceipt is not null)
        {
            AddAudit(
                downloadIntentId: intent.Id,
                downloadReceiptId: existingReceipt.Id,
                action: "download_receipt_duplicate_ignored",
                payload: new
                {
                    IntentId = intent.Id,
                    ExistingReceiptId = existingReceipt.Id,
                    ClientReceiptKey = normalizedClientReceiptKey
                });

            await dbContext.SaveChangesAsync(cancellationToken);

            return new DownloadReceiptResponseV1Dto(
                DownloadReceiptId: existingReceipt.Id,
                DownloadIntentId: intent.Id,
                Status: DownloadReceiptV1Statuses.DuplicateIgnored,
                DuplicateIgnored: true,
                AcceptedAtUtc: existingReceipt.AcceptedAtUtc);
        }

        var receipt = new VideoDownloadReceipt
        {
            Id = Guid.NewGuid(),
            DownloadIntentId = intent.Id,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            ExternalVideoId = request.ExternalVideoId.Trim(),
            ClientReceiptKey = normalizedClientReceiptKey,
            DownloadedBytes = request.DownloadedBytes,
            Status = DownloadReceiptV1Statuses.Accepted,
            DownloadedAtUtc = request.DownloadedAtUtc,
            AcceptedAtUtc = now
        };

        intent.Status = DownloadIntentV1Statuses.Consumed;
        intent.ConsumedAtUtc = now;

        dbContext.VideoDownloadReceipts.Add(receipt);

        AddAudit(
            downloadIntentId: intent.Id,
            downloadReceiptId: receipt.Id,
            action: "download_receipt_accepted",
            payload: new
            {
                ReceiptId = receipt.Id,
                IntentId = intent.Id,
                receipt.UserId,
                receipt.DeviceId,
                receipt.ExternalVideoId,
                receipt.ClientReceiptKey,
                receipt.DownloadedBytes,
                receipt.DownloadedAtUtc
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Download receipt accepted. ReceiptId={ReceiptId} IntentId={IntentId}",
            receipt.Id,
            intent.Id);

        return new DownloadReceiptResponseV1Dto(
            DownloadReceiptId: receipt.Id,
            DownloadIntentId: intent.Id,
            Status: DownloadReceiptV1Statuses.Accepted,
            DuplicateIgnored: false,
            AcceptedAtUtc: receipt.AcceptedAtUtc);
    }

    public async Task<DownloadIntentSummaryV1Dto?> GetIntentAsync(
        Guid downloadIntentId,
        CancellationToken cancellationToken)
    {
        if (downloadIntentId == Guid.Empty)
        {
            throw new ArgumentException("DownloadIntentId is required.", nameof(downloadIntentId));
        }

        var intent = await dbContext.VideoDownloadIntents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == downloadIntentId, cancellationToken);

        return intent is null ? null : ToIntentSummary(intent);
    }

    public async Task<DownloadReceiptSummaryV1Dto?> GetReceiptAsync(
        Guid downloadReceiptId,
        CancellationToken cancellationToken)
    {
        if (downloadReceiptId == Guid.Empty)
        {
            throw new ArgumentException("DownloadReceiptId is required.", nameof(downloadReceiptId));
        }

        var receipt = await dbContext.VideoDownloadReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == downloadReceiptId, cancellationToken);

        return receipt is null ? null : ToReceiptSummary(receipt);
    }

    private void AddAudit(
        Guid? downloadIntentId,
        Guid? downloadReceiptId,
        string action,
        object payload)
    {
        dbContext.VideoDownloadAuditRecords.Add(new VideoDownloadAuditRecord
        {
            Id = Guid.NewGuid(),
            DownloadIntentId = downloadIntentId,
            DownloadReceiptId = downloadReceiptId,
            Category = "video_download",
            Action = action,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private static string BuildDownloadUrl(
        Guid intentId,
        string externalVideoId,
        VideoDownloadOptions options)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.StubDownloadBaseUrl)
            ? "https://stub-video-site.local/download"
            : options.StubDownloadBaseUrl.Trim().TrimEnd('/');

        return $"{baseUrl}/{Uri.EscapeDataString(externalVideoId)}?intentId={Uri.EscapeDataString(intentId.ToString("N"))}";
    }

    private static CreateDownloadIntentResponseV1Dto ToIntentResponse(VideoDownloadIntent intent)
    {
        return new CreateDownloadIntentResponseV1Dto(
            DownloadIntentId: intent.Id,
            UserId: intent.UserId,
            DeviceId: intent.DeviceId,
            GroupNodeId: intent.GroupNodeId,
            VideoAssetId: intent.VideoAssetId,
            ExternalVideoId: intent.ExternalVideoId,
            BusinessObjectKey: intent.BusinessObjectKey,
            Status: intent.Status,
            SiteProvider: intent.SiteProvider,
            DownloadUrl: intent.DownloadUrl,
            CreatedAtUtc: intent.CreatedAtUtc,
            ExpiresAtUtc: intent.ExpiresAtUtc);
    }

    private static DownloadIntentSummaryV1Dto ToIntentSummary(VideoDownloadIntent intent)
    {
        return new DownloadIntentSummaryV1Dto(
            DownloadIntentId: intent.Id,
            UserId: intent.UserId,
            DeviceId: intent.DeviceId,
            GroupNodeId: intent.GroupNodeId,
            VideoAssetId: intent.VideoAssetId,
            ExternalVideoId: intent.ExternalVideoId,
            BusinessObjectKey: intent.BusinessObjectKey,
            Status: intent.Status,
            SiteProvider: intent.SiteProvider,
            DownloadUrl: intent.DownloadUrl,
            CreatedAtUtc: intent.CreatedAtUtc,
            ExpiresAtUtc: intent.ExpiresAtUtc,
            ConsumedAtUtc: intent.ConsumedAtUtc,
            RejectedReason: intent.RejectedReason);
    }

    private static DownloadReceiptSummaryV1Dto ToReceiptSummary(VideoDownloadReceipt receipt)
    {
        return new DownloadReceiptSummaryV1Dto(
            DownloadReceiptId: receipt.Id,
            DownloadIntentId: receipt.DownloadIntentId,
            UserId: receipt.UserId,
            DeviceId: receipt.DeviceId,
            ExternalVideoId: receipt.ExternalVideoId,
            ClientReceiptKey: receipt.ClientReceiptKey,
            DownloadedBytes: receipt.DownloadedBytes,
            Status: receipt.Status,
            DownloadedAtUtc: receipt.DownloadedAtUtc,
            AcceptedAtUtc: receipt.AcceptedAtUtc);
    }

    private static void ValidateIntentRequest(CreateDownloadIntentRequestV1Dto request)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (request.GroupNodeId == Guid.Empty)
        {
            throw new ArgumentException("GroupNodeId is required.", nameof(request));
        }

        if (request.VideoAssetId == Guid.Empty)
        {
            throw new ArgumentException("VideoAssetId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ExternalVideoId))
        {
            throw new ArgumentException("ExternalVideoId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.BusinessObjectKey))
        {
            throw new ArgumentException("BusinessObjectKey is required.", nameof(request));
        }
    }

    private static void ValidateReceiptRequest(DownloadReceiptRequestV1Dto request)
    {
        if (request.DownloadIntentId == Guid.Empty)
        {
            throw new ArgumentException("DownloadIntentId is required.", nameof(request));
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ExternalVideoId))
        {
            throw new ArgumentException("ExternalVideoId is required.", nameof(request));
        }

        if (request.DownloadedBytes.HasValue && request.DownloadedBytes.Value < 0)
        {
            throw new ArgumentException("DownloadedBytes cannot be negative.", nameof(request));
        }
    }
}

public static class VideoDownloadModule
{
    public static IServiceCollection AddVideoDownloadModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<VideoDownloadOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(VideoDownloadOptions.SectionName);

                if (bool.TryParse(section["Enabled"], out var enabled))
                {
                    options.Enabled = enabled;
                }

                if (int.TryParse(section["DefaultIntentTtlSeconds"], out var ttl))
                {
                    options.DefaultIntentTtlSeconds = ttl;
                }

                if (!string.IsNullOrWhiteSpace(section["SiteProvider"]))
                {
                    options.SiteProvider = section["SiteProvider"]!;
                }

                if (!string.IsNullOrWhiteSpace(section["StubDownloadBaseUrl"]))
                {
                    options.StubDownloadBaseUrl = section["StubDownloadBaseUrl"]!;
                }
            })
            .Validate(options => options.DefaultIntentTtlSeconds is >= 60 and <= 86400, "DefaultIntentTtlSeconds must be between 60 and 86400.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SiteProvider), "SiteProvider is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.StubDownloadBaseUrl), "StubDownloadBaseUrl is required.")
            .ValidateOnStart();

        services.AddScoped<IVideoDownloadService, VideoDownloadService>();

        return services;
    }

    public static IEndpointRouteBuilder MapVideoDownloadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/video-download/intents",
            async Task<IResult> (
                CreateDownloadIntentRequestV1Dto request,
                IVideoDownloadService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.CreateDownloadIntentAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Download intent rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_download_intent_request",
                        message = exception.Message
                    });
                }
            });

        endpoints.MapPost(
            "/api/video-download/receipts",
            async Task<IResult> (
                DownloadReceiptRequestV1Dto request,
                IVideoDownloadService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.AcceptDownloadReceiptAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Download receipt rejected");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_download_receipt_request",
                        message = exception.Message
                    });
                }
            });

        endpoints.MapGet(
            "/api/video-download/intents/{downloadIntentId:guid}",
            async Task<IResult> (
                Guid downloadIntentId,
                IVideoDownloadService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetIntentAsync(downloadIntentId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

        endpoints.MapGet(
            "/api/video-download/receipts/{downloadReceiptId:guid}",
            async Task<IResult> (
                Guid downloadReceiptId,
                IVideoDownloadService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetReceiptAsync(downloadReceiptId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

        endpoints.MapGet(
            "/api/video-download/status-vocabulary",
            () => Results.Ok(new DownloadControlPlaneStatusV1Dto(
                IntentStatuses: new[]
                {
                    DownloadIntentV1Statuses.Created,
                    DownloadIntentV1Statuses.Consumed,
                    DownloadIntentV1Statuses.Expired,
                    DownloadIntentV1Statuses.Rejected
                },
                ReceiptStatuses: new[]
                {
                    DownloadReceiptV1Statuses.Accepted,
                    DownloadReceiptV1Statuses.DuplicateIgnored
                })));

        return endpoints;
    }
}