namespace BuildingBlocks.Contracts.WebsiteIntegration;

public sealed record SiteUploadReceiptContractDto(
    IReadOnlyCollection<string> RequiredFields,
    string ExternalVideoIdFormat,
    string StorageKeyFormat,
    IReadOnlyCollection<string> KnownStatuses);

public sealed record SiteDownloadIntentContractDto(
    IReadOnlyCollection<string> ResponseFields,
    IReadOnlyCollection<string> KnownStatuses);

public sealed record SiteReconcileContractDto(
    string RequestField,
    string ResponseCollectionField,
    IReadOnlyCollection<string> KnownStatuses);

public sealed record SiteStatusLookupContractDto(
    string RouteTemplate,
    string ResponseField,
    IReadOnlyCollection<string> KnownStatuses);

public sealed record SiteGatewayContractsDto(
    string Provider,
    string Mode,
    SiteUploadReceiptContractDto UploadReceipt,
    SiteDownloadIntentContractDto DownloadIntent,
    SiteReconcileContractDto Reconcile,
    SiteStatusLookupContractDto? StatusLookup = null);

public sealed record SiteUploadReceiptPreviewRequestDto(
    string ExternalVideoId,
    string? StorageKey,
    string SiteStatus,
    DateTimeOffset UploadedAtUtc);

public sealed record SiteUploadReceiptPreviewResponseDto(
    string Provider,
    string ExternalVideoId,
    string StorageKey,
    string SiteStatus,
    bool Accepted,
    DateTimeOffset ReceivedAtUtc);

public sealed record SiteDownloadIntentPreviewRequestDto(
    string ExternalVideoId);

public sealed record SiteDownloadIntentPreviewResponseDto(
    string Provider,
    string ExternalVideoId,
    string StorageKey,
    string SiteStatus,
    string DownloadUrl,
    DateTimeOffset ExpiresAtUtc);

public sealed record ReconcileSiteVideosRequestDto(
    IReadOnlyCollection<string> ExternalVideoIds);

public sealed record SiteVideoStatusDto(
    string ExternalVideoId,
    string StorageKey,
    string Status,
    DateTimeOffset CheckedAtUtc);

public sealed record ReconcileSiteVideosResponseDto(
    IReadOnlyCollection<SiteVideoStatusDto> Items);