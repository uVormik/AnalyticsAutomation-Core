using System.Globalization;
using System.Text.Json;

using BuildingBlocks.Contracts.VideoDuplicates;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.VideoDuplicates;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.VideoDuplicates;

public sealed class VideoDuplicatesOptions
{
    public const string SectionName = "Modules:VideoDuplicates";

    public bool RegistryEnabled { get; set; } = true;
    public bool ExactFingerprintEnabled { get; set; } = true;
}

public interface IVideoDuplicateRegistryService
{
    Task<DuplicateAssetRegistrationResponseDto> RegisterAssetAsync(
        DuplicateAssetRegistrationRequestDto request,
        CancellationToken cancellationToken);

    Task<DuplicateCandidateLookupResponseDto> GetCandidatesAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken);
}

public sealed class VideoDuplicateRegistryService(
    PlatformDbContext dbContext,
    IOptions<VideoDuplicatesOptions> options,
    ILogger<VideoDuplicateRegistryService> logger) : IVideoDuplicateRegistryService
{
    public async Task<DuplicateAssetRegistrationResponseDto> RegisterAssetAsync(
        DuplicateAssetRegistrationRequestDto request,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        if (!value.RegistryEnabled)
        {
            throw new InvalidOperationException("Duplicate registry is disabled.");
        }

        ValidateRequest(request);

        var normalizedHash = request.ByteSha256.Trim().ToLowerInvariant();
        var normalizedBusinessObjectKey = request.BusinessObjectKey.Trim();
        var normalizedExternalVideoId = request.ExternalVideoId.Trim();
        var normalizedStorageKey = request.StorageKey.Trim();

        var existingAsset = await dbContext.VideoDuplicateAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UploadReceiptId == request.UploadReceiptId,
                cancellationToken);

        if (existingAsset is not null)
        {
            var existingCandidates = await GetCandidateDtosAsync(existingAsset.Id, cancellationToken);

            return new DuplicateAssetRegistrationResponseDto(
                VideoAssetId: existingAsset.Id,
                Registered: false,
                Candidates: existingCandidates,
                RegisteredAtUtc: existingAsset.RegisteredAtUtc);
        }

        var registeredAtUtc = DateTimeOffset.UtcNow;
        var assetId = Guid.NewGuid();

        var asset = new VideoDuplicateAsset
        {
            Id = assetId,
            UploadReceiptId = request.UploadReceiptId,
            PreUploadCheckId = request.PreUploadCheckId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            GroupNodeId = request.GroupNodeId,
            BusinessObjectKey = normalizedBusinessObjectKey,
            ExternalVideoId = normalizedExternalVideoId,
            StorageKey = normalizedStorageKey,
            SizeBytes = request.SizeBytes,
            ByteSha256 = normalizedHash,
            UploadedAtUtc = request.UploadedAtUtc,
            RegisteredAtUtc = registeredAtUtc,
            IsActive = true
        };

        var fingerprint = new VideoDuplicateFingerprint
        {
            Id = Guid.NewGuid(),
            VideoAssetId = assetId,
            FingerprintKind = "byte_sha256_size",
            FingerprintValue = normalizedHash,
            SizeBytes = request.SizeBytes,
            CreatedAtUtc = registeredAtUtc
        };

        var candidates = new List<VideoDuplicateCandidate>();

        if (value.ExactFingerprintEnabled)
        {
            var existingMatches = await dbContext.VideoDuplicateAssets
                .AsNoTracking()
                .Where(item =>
                    item.IsActive &&
                    item.ByteSha256 == normalizedHash &&
                    item.SizeBytes == request.SizeBytes)
                .OrderBy(item => item.RegisteredAtUtc)
                .ToListAsync(cancellationToken);

            foreach (var match in existingMatches)
            {
                candidates.Add(new VideoDuplicateCandidate
                {
                    Id = Guid.NewGuid(),
                    SourceVideoAssetId = assetId,
                    MatchedVideoAssetId = match.Id,
                    MatchKind = DuplicateMatchKinds.HardDuplicate,
                    ReasonCode = "exact_byte_sha256_size_match",
                    Decision = DuplicateCandidateDecisions.Candidate,
                    DetectedAtUtc = registeredAtUtc
                });
            }
        }

        var auditPayload = JsonSerializer.Serialize(new
        {
            asset.Id,
            asset.UploadReceiptId,
            asset.PreUploadCheckId,
            asset.UserId,
            asset.DeviceId,
            asset.GroupNodeId,
            asset.BusinessObjectKey,
            asset.ExternalVideoId,
            asset.StorageKey,
            asset.ByteSha256,
            asset.SizeBytes,
            CandidateCount = candidates.Count
        });

        var audit = new VideoDuplicateRegistryAuditRecord
        {
            Id = Guid.NewGuid(),
            VideoAssetId = assetId,
            DuplicateCandidateId = candidates.Count == 1 ? candidates[0].Id : null,
            Category = "video_duplicates",
            Action = candidates.Count == 0 ? "asset_registered" : "duplicate_candidate_found",
            PayloadJson = auditPayload,
            CreatedAtUtc = registeredAtUtc
        };

        dbContext.VideoDuplicateAssets.Add(asset);
        dbContext.VideoDuplicateFingerprints.Add(fingerprint);
        dbContext.VideoDuplicateCandidates.AddRange(candidates);
        dbContext.VideoDuplicateRegistryAuditRecords.Add(audit);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Video asset registered in duplicate registry. VideoAssetId={VideoAssetId} CandidateCount={CandidateCount}",
            assetId,
            candidates.Count);

        return new DuplicateAssetRegistrationResponseDto(
            VideoAssetId: assetId,
            Registered: true,
            Candidates: candidates.Select(ToDto).ToArray(),
            RegisteredAtUtc: registeredAtUtc);
    }

    public async Task<DuplicateCandidateLookupResponseDto> GetCandidatesAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken)
    {
        if (videoAssetId == Guid.Empty)
        {
            throw new ArgumentException("VideoAssetId is required.", nameof(videoAssetId));
        }

        var candidates = await GetCandidateDtosAsync(videoAssetId, cancellationToken);

        return new DuplicateCandidateLookupResponseDto(
            VideoAssetId: videoAssetId,
            Candidates: candidates);
    }

    private async Task<IReadOnlyCollection<DuplicateCandidateDto>> GetCandidateDtosAsync(
        Guid videoAssetId,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.VideoDuplicateCandidates
            .AsNoTracking()
            .Where(item => item.SourceVideoAssetId == videoAssetId || item.MatchedVideoAssetId == videoAssetId)
            .OrderBy(item => item.DetectedAtUtc)
            .ToListAsync(cancellationToken);

        return candidates.Select(ToDto).ToArray();
    }

    private static DuplicateCandidateDto ToDto(VideoDuplicateCandidate candidate)
    {
        return new DuplicateCandidateDto(
            CandidateId: candidate.Id,
            SourceVideoAssetId: candidate.SourceVideoAssetId,
            MatchedVideoAssetId: candidate.MatchedVideoAssetId,
            MatchKind: candidate.MatchKind,
            ReasonCode: candidate.ReasonCode,
            Decision: candidate.Decision,
            DetectedAtUtc: candidate.DetectedAtUtc);
    }

    private static void ValidateRequest(DuplicateAssetRegistrationRequestDto request)
    {
        if (request.UploadReceiptId == Guid.Empty)
        {
            throw new ArgumentException("UploadReceiptId is required.", nameof(request));
        }

        if (request.PreUploadCheckId == Guid.Empty)
        {
            throw new ArgumentException("PreUploadCheckId is required.", nameof(request));
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.BusinessObjectKey))
        {
            throw new ArgumentException("BusinessObjectKey is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ExternalVideoId))
        {
            throw new ArgumentException("ExternalVideoId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(request));
        }

        if (request.SizeBytes <= 0)
        {
            throw new ArgumentException("SizeBytes must be positive.", nameof(request));
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

public static class VideoDuplicatesModule
{
    public static IServiceCollection AddVideoDuplicatesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<VideoDuplicatesOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(VideoDuplicatesOptions.SectionName);

                if (bool.TryParse(section["RegistryEnabled"], out var registryEnabled))
                {
                    options.RegistryEnabled = registryEnabled;
                }

                if (bool.TryParse(section["ExactFingerprintEnabled"], out var exactFingerprintEnabled))
                {
                    options.ExactFingerprintEnabled = exactFingerprintEnabled;
                }
            })
            .ValidateOnStart();

        services.AddScoped<IVideoDuplicateRegistryService, VideoDuplicateRegistryService>();

        return services;
    }

    public static IEndpointRouteBuilder MapVideoDuplicatesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/video-duplicates/register-asset",
            async Task<IResult> (
                DuplicateAssetRegistrationRequestDto request,
                IVideoDuplicateRegistryService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.RegisterAssetAsync(request, cancellationToken);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException exception)
                {
                    return Results.Problem(
                        detail: exception.Message,
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Duplicate registry unavailable");
                }
                catch (ArgumentException exception)
                {
                    return Results.BadRequest(new
                    {
                        error = "invalid_duplicate_asset_registration_request",
                        message = exception.Message
                    });
                }
            });

        endpoints.MapGet(
            "/api/video-duplicates/assets/{videoAssetId:guid}/candidates",
            async Task<IResult> (
                Guid videoAssetId,
                IVideoDuplicateRegistryService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetCandidatesAsync(videoAssetId, cancellationToken);
                return Results.Ok(result);
            });

        return endpoints;
    }
}