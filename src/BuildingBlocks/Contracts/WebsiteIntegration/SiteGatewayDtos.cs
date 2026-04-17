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

public sealed record SiteGatewayContractsDto(
    string Provider,
    string Mode,
    SiteUploadReceiptContractDto UploadReceipt,
    SiteDownloadIntentContractDto DownloadIntent,
    SiteReconcileContractDto Reconcile);

public sealed record ReconcileSiteVideosRequestDto(
    IReadOnlyCollection<string> ExternalVideoIds);

public sealed record SiteVideoStatusDto(
    string ExternalVideoId,
    string StorageKey,
    string Status,
    DateTimeOffset CheckedAtUtc);

public sealed record ReconcileSiteVideosResponseDto(
    IReadOnlyCollection<SiteVideoStatusDto> Items);