using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.Auth;

public sealed record CurrentUserSummaryDto(
    Guid UserId,
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    Guid? CurrentGroupNodeId);

public sealed record SessionInfoDto(
    Guid SessionId,
    Guid UserId,
    Guid? DeviceId,
    bool IsOfflineRestricted,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record SignInRequestDto(
    string Login,
    string Password,
    Guid? DeviceId);

public sealed record SignInResponseDto(
    CurrentUserSummaryDto User,
    SessionInfoDto Session,
    string? AccessToken,
    string? RefreshToken);

public sealed record RefreshSessionRequestDto(
    Guid SessionId,
    Guid? DeviceId,
    string? RefreshToken);

public sealed record RefreshSessionResponseDto(
    CurrentUserSummaryDto User,
    SessionInfoDto Session,
    string? AccessToken,
    string? RefreshToken);