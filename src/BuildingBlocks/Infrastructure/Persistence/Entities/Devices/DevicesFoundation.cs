namespace BuildingBlocks.Infrastructure.Persistence.Entities.Devices;

public sealed class DeviceRegistration
{
    public Guid DeviceId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? OsVersion { get; set; }
    public bool IsTrusted { get; set; }
    public Guid? LastKnownUserId { get; set; }
    public DateTimeOffset RegisteredAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
}