using BuildingBlocks.Contracts.WebsiteIntegration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.WebsiteIntegration;

public sealed class WebsiteIntegrationOptions
{
    public const string SectionName = "Modules:WebsiteIntegration";

    public string Provider { get; init; } = "Stub";
    public string StubMode { get; init; } = "Development";
    public string ExternalVideoIdPrefix { get; init; } = "site-video";
    public string StorageKeyPrefix { get; init; } = "videos";
}

public interface IWebsiteVideoGateway
{
    Task<SiteGatewayContractsDto> GetContractsAsync(CancellationToken cancellationToken);

    Task<SiteUploadReceiptPreviewResponseDto> AcceptUploadReceiptPreviewAsync(
        SiteUploadReceiptPreviewRequestDto request,
        CancellationToken cancellationToken);

    Task<SiteDownloadIntentPreviewResponseDto> CreateDownloadIntentPreviewAsync(
        SiteDownloadIntentPreviewRequestDto request,
        CancellationToken cancellationToken);

    Task<SiteVideoStatusDto> GetStatusAsync(
        string externalVideoId,
        CancellationToken cancellationToken);

    Task<ReconcileSiteVideosResponseDto> ReconcileAsync(
        ReconcileSiteVideosRequestDto request,
        CancellationToken cancellationToken);
}

internal sealed class StubWebsiteVideoGateway(
    IOptions<WebsiteIntegrationOptions> options,
    ILogger<StubWebsiteVideoGateway> logger) : IWebsiteVideoGateway
{
    private static readonly string[] KnownStatuses =
    [
        "uploaded",
        "available",
        "deleted"
    ];

    public Task<SiteGatewayContractsDto> GetContractsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = options.Value;

        logger.LogInformation(
            "Website gateway contracts requested. Provider={Provider} Mode={Mode}",
            value.Provider,
            value.StubMode);

        return Task.FromResult(
            new SiteGatewayContractsDto(
                Provider: value.Provider,
                Mode: value.StubMode,
                UploadReceipt: new SiteUploadReceiptContractDto(
                    RequiredFields:
                    [
                        "provider",
                        "externalVideoId",
                        "storageKey",
                        "siteStatus"
                    ],
                    ExternalVideoIdFormat: $"{value.ExternalVideoIdPrefix}-<guid>",
                    StorageKeyFormat: $"{value.StorageKeyPrefix}/<externalVideoId>.mp4",
                    KnownStatuses: KnownStatuses),
                DownloadIntent: new SiteDownloadIntentContractDto(
                    ResponseFields:
                    [
                        "externalVideoId",
                        "storageKey",
                        "siteStatus",
                        "downloadUrl",
                        "expiresAtUtc"
                    ],
                    KnownStatuses: KnownStatuses),
                Reconcile: new SiteReconcileContractDto(
                    RequestField: "externalVideoIds",
                    ResponseCollectionField: "items",
                    KnownStatuses: KnownStatuses),
                StatusLookup: new SiteStatusLookupContractDto(
                    RouteTemplate: "/api/system/site-gateway/status/{externalVideoId}",
                    ResponseField: "siteStatus",
                    KnownStatuses: KnownStatuses)));
    }

    public Task<SiteUploadReceiptPreviewResponseDto> AcceptUploadReceiptPreviewAsync(
        SiteUploadReceiptPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = options.Value;
        var externalVideoId = EnsureExternalVideoId(request.ExternalVideoId);
        var storageKey = EnsureStorageKey(request.StorageKey, externalVideoId);
        var siteStatus = NormalizeStatus(request.SiteStatus);

        logger.LogInformation(
            "Website gateway upload receipt preview accepted. Provider={Provider} ExternalVideoId={ExternalVideoId}",
            value.Provider,
            externalVideoId);

        return Task.FromResult(
            new SiteUploadReceiptPreviewResponseDto(
                Provider: value.Provider,
                ExternalVideoId: externalVideoId,
                StorageKey: storageKey,
                SiteStatus: siteStatus,
                Accepted: true,
                ReceivedAtUtc: DateTimeOffset.UtcNow));
    }

    public Task<SiteDownloadIntentPreviewResponseDto> CreateDownloadIntentPreviewAsync(
        SiteDownloadIntentPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = options.Value;
        var externalVideoId = EnsureExternalVideoId(request.ExternalVideoId);
        var storageKey = BuildStorageKey(externalVideoId);

        logger.LogInformation(
            "Website gateway download intent preview created. Provider={Provider} ExternalVideoId={ExternalVideoId}",
            value.Provider,
            externalVideoId);

        return Task.FromResult(
            new SiteDownloadIntentPreviewResponseDto(
                Provider: value.Provider,
                ExternalVideoId: externalVideoId,
                StorageKey: storageKey,
                SiteStatus: "available",
                DownloadUrl: $"https://stub-site.invalid/download/{Uri.EscapeDataString(externalVideoId)}",
                ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    public Task<SiteVideoStatusDto> GetStatusAsync(
        string externalVideoId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedExternalVideoId = EnsureExternalVideoId(externalVideoId);

        return Task.FromResult(
            new SiteVideoStatusDto(
                ExternalVideoId: normalizedExternalVideoId,
                StorageKey: BuildStorageKey(normalizedExternalVideoId),
                Status: "available",
                CheckedAtUtc: DateTimeOffset.UtcNow));
    }

    public async Task<ReconcileSiteVideosResponseDto> ReconcileAsync(
        ReconcileSiteVideosRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = new List<SiteVideoStatusDto>();

        foreach (var externalVideoId in request.ExternalVideoIds
                     .Where(item => !string.IsNullOrWhiteSpace(item))
                     .Select(item => item.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            items.Add(await GetStatusAsync(externalVideoId, cancellationToken));
        }

        logger.LogInformation(
            "Website gateway reconcile preview executed. Provider={Provider} RequestedIds={RequestedIds}",
            options.Value.Provider,
            items.Count);

        return new ReconcileSiteVideosResponseDto(items);
    }

    private string EnsureExternalVideoId(string externalVideoId)
    {
        if (string.IsNullOrWhiteSpace(externalVideoId))
        {
            throw new ArgumentException("ExternalVideoId is required.", nameof(externalVideoId));
        }

        return externalVideoId.Trim();
    }

    private string EnsureStorageKey(string? storageKey, string externalVideoId)
    {
        if (!string.IsNullOrWhiteSpace(storageKey))
        {
            return storageKey.Trim();
        }

        return BuildStorageKey(externalVideoId);
    }

    private string BuildStorageKey(string externalVideoId)
    {
        return $"{options.Value.StorageKeyPrefix}/{externalVideoId}.mp4";
    }

    private static string NormalizeStatus(string siteStatus)
    {
        if (string.IsNullOrWhiteSpace(siteStatus))
        {
            return "uploaded";
        }

        var normalized = siteStatus.Trim().ToLowerInvariant();
        if (!KnownStatuses.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Unknown site status.", nameof(siteStatus));
        }

        return normalized;
    }
}

public static class WebsiteIntegrationModule
{
    public static IServiceCollection AddWebsiteIntegrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<WebsiteIntegrationOptions>()
            .Bind(configuration.GetSection(WebsiteIntegrationOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Provider),
                "Modules:WebsiteIntegration:Provider is required.")
            .Validate(
                options => string.Equals(options.Provider, "Stub", StringComparison.OrdinalIgnoreCase),
                "Only Stub provider is supported in Sprint 1 baseline.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ExternalVideoIdPrefix),
                "Modules:WebsiteIntegration:ExternalVideoIdPrefix is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.StorageKeyPrefix),
                "Modules:WebsiteIntegration:StorageKeyPrefix is required.")
            .ValidateOnStart();

        services.AddSingleton<IWebsiteVideoGateway, StubWebsiteVideoGateway>();

        return services;
    }

    public static IEndpointRouteBuilder MapWebsiteIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/system/site-gateway",
            async Task<IResult> (
                IWebsiteVideoGateway gateway,
                CancellationToken cancellationToken) =>
            {
                var result = await gateway.GetContractsAsync(cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapPost(
            "/api/system/site-gateway/upload-receipt-preview",
            async Task<IResult> (
                SiteUploadReceiptPreviewRequestDto request,
                IWebsiteVideoGateway gateway,
                CancellationToken cancellationToken) =>
            {
                var result = await gateway.AcceptUploadReceiptPreviewAsync(request, cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapPost(
            "/api/system/site-gateway/download-intent-preview",
            async Task<IResult> (
                SiteDownloadIntentPreviewRequestDto request,
                IWebsiteVideoGateway gateway,
                CancellationToken cancellationToken) =>
            {
                var result = await gateway.CreateDownloadIntentPreviewAsync(request, cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapGet(
            "/api/system/site-gateway/status/{externalVideoId}",
            async Task<IResult> (
                string externalVideoId,
                IWebsiteVideoGateway gateway,
                CancellationToken cancellationToken) =>
            {
                var result = await gateway.GetStatusAsync(externalVideoId, cancellationToken);
                return Results.Ok(result);
            });

        endpoints.MapPost(
            "/api/system/site-gateway/reconcile-preview",
            async Task<IResult> (
                ReconcileSiteVideosRequestDto request,
                IWebsiteVideoGateway gateway,
                CancellationToken cancellationToken) =>
            {
                var result = await gateway.ReconcileAsync(request, cancellationToken);
                return Results.Ok(result);
            });

        return endpoints;
    }
}