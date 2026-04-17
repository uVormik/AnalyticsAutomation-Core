namespace BuildingBlocks.Contracts.Devices;

public sealed record DeviceRegistrationUpsertRequestDto(
    Guid DeviceId,
    string Platform,
    string DeviceName,
    string? Model,
    string? OsVersion,
    bool IsTrusted,
    Guid? LastKnownUserId);

public sealed record DeviceRegistrationInfoDto(
    Guid DeviceId,
    string Platform,
    string DeviceName,
    string? Model,
    string? OsVersion,
    bool IsTrusted,
    Guid? LastKnownUserId,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc);