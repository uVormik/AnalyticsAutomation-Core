using System;

namespace BuildingBlocks.Contracts.Devices;

public enum DevicePlatform
{
    Unknown = 0,
    DesktopPwa = 1,
    Android = 2,
    Web = 3
}

public sealed record DeviceRegistrationRequestDto(
    Guid DeviceId,
    DevicePlatform Platform,
    string? DeviceName,
    string? Model,
    string? OsVersion,
    string? AppVersion);

public sealed record DeviceRegistrationResponseDto(
    Guid DeviceId,
    DevicePlatform Platform,
    bool IsTrusted,
    Guid? LastActiveUserId,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset LastSeenAtUtc);

public sealed record DeviceSummaryDto(
    Guid DeviceId,
    DevicePlatform Platform,
    string? DeviceName,
    bool IsTrusted,
    Guid? LastActiveUserId,
    DateTimeOffset LastSeenAtUtc);