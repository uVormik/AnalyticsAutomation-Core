namespace BuildingBlocks.Infrastructure.Persistence.Entities.VideoUpload;

public sealed class VideoUploadPreUploadCheck
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid? GroupNodeId { get; set; }
    public string BusinessObjectKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ByteSha256 { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string Decision { get; set; } = string.Empty;
    public bool CanUploadToSite { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public Guid? ExistingPreUploadCheckId { get; set; }
    public string RequiredNextSteps { get; set; } = string.Empty;
    public string? SiteProvider { get; set; }
    public string? ExternalVideoId { get; set; }
    public string? StorageKey { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
}