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
                value.Provider,
                value.StubMode,
                new SiteUploadReceiptContractDto(
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
                new SiteDownloadIntentContractDto(
                    ResponseFields:
                    [
                        "externalVideoId",
                        "storageKey",
                        "siteStatus",
                        "expiresAtUtc"
                    ],
                    KnownStatuses: KnownStatuses),
                new SiteReconcileContractDto(
                    RequestField: "externalVideoIds",
                    ResponseCollectionField: "items",
                    KnownStatuses: KnownStatuses)));
    }

    public Task<ReconcileSiteVideosResponseDto> ReconcileAsync(
        ReconcileSiteVideosRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = options.Value;
        var now = DateTimeOffset.UtcNow;

        var items = request.ExternalVideoIds
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(item => new SiteVideoStatusDto(
                ExternalVideoId: item,
                StorageKey: $"{value.StorageKeyPrefix}/{item}.mp4",
                Status: "available",
                CheckedAtUtc: now))
            .ToArray();

        logger.LogInformation(
            "Website gateway reconcile preview executed. Provider={Provider} RequestedIds={RequestedIds}",
            value.Provider,
            items.Length);

        return Task.FromResult(new ReconcileSiteVideosResponseDto(items));
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
                "Only Stub provider is supported in Sprint 0.")
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