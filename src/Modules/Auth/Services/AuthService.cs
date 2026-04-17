using System.Security.Cryptography;
using System.Text;

using BuildingBlocks.Contracts.Auth;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Modules.Auth.Configuration;

namespace Modules.Auth.Services;

public sealed class AuthService(
    PlatformDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher,
    IOptions<AuthOptions> authOptions,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<SignInResponseDto?> SignInAsync(
        SignInRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalizedLogin = NormalizeLogin(request.Login);

        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
                .ThenInclude(item => item.Role)
                    .ThenInclude(item => item.RolePermissions)
                        .ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(
                item => item.NormalizedLogin == normalizedLogin,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            logger.LogWarning(
                "Sign-in failed for Login={Login}. User was not found or inactive.",
                request.Login);
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning(
                "Sign-in failed for Login={Login}. Password verification failed.",
                request.Login);
            return null;
        }

        var response = await CreateSessionAsync(
            user,
            request.DeviceId,
            cancellationToken);

        logger.LogInformation(
            "Sign-in succeeded for UserId={UserId} DeviceId={DeviceId}.",
            user.Id,
            request.DeviceId);

        return response;
    }

    public async Task<RefreshSessionResponseDto?> RefreshAsync(
        RefreshSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logger.LogWarning(
                "Refresh failed for SessionId={SessionId}. Refresh token was empty.",
                request.SessionId);
            return null;
        }

        var refreshTokenHash = ComputeHash(request.RefreshToken);
        var session = await dbContext.AuthSessions
            .Include(item => item.User)
                .ThenInclude(item => item.UserRoles)
                    .ThenInclude(item => item.Role)
                        .ThenInclude(item => item.RolePermissions)
                            .ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(
                item => item.Id == request.SessionId
                    && item.RefreshTokenHash == refreshTokenHash,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (session is null
            || session.RevokedAtUtc is not null
            || session.RefreshExpiresAtUtc <= now)
        {
            logger.LogWarning(
                "Refresh failed for SessionId={SessionId}. Session was not found or expired.",
                request.SessionId);
            return null;
        }

        var accessToken = CreateOpaqueToken();
        var refreshToken = CreateOpaqueToken();

        session.AccessTokenHash = ComputeHash(accessToken);
        session.RefreshTokenHash = ComputeHash(refreshToken);
        session.DeviceId = request.DeviceId ?? session.DeviceId;
        session.IssuedAtUtc = now;
        session.ExpiresAtUtc = now.AddMinutes(authOptions.Value.AccessTokenLifetimeMinutes);
        session.RefreshExpiresAtUtc = now.AddHours(authOptions.Value.RefreshTokenLifetimeHours);
        session.IsOfflineRestricted = authOptions.Value.OfflineRestrictedModeEnabled;

        if (session.DeviceId.HasValue)
        {
            await UpsertLastActiveDeviceAccountAsync(
                session.DeviceId.Value,
                session.UserId,
                session.IsOfflineRestricted,
                now,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Session refresh succeeded for SessionId={SessionId}.",
            session.Id);

        return new RefreshSessionResponseDto(
            MapUser(session.User),
            MapSession(session),
            accessToken,
            refreshToken);
    }

    private async Task<SignInResponseDto> CreateSessionAsync(
        AuthUser user,
        Guid? deviceId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var accessToken = CreateOpaqueToken();
        var refreshToken = CreateOpaqueToken();

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceId = deviceId,
            AccessTokenHash = ComputeHash(accessToken),
            RefreshTokenHash = ComputeHash(refreshToken),
            IsOfflineRestricted = authOptions.Value.OfflineRestrictedModeEnabled,
            IssuedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(authOptions.Value.AccessTokenLifetimeMinutes),
            RefreshExpiresAtUtc = now.AddHours(authOptions.Value.RefreshTokenLifetimeHours)
        };

        dbContext.AuthSessions.Add(session);

        user.LastSignInAtUtc = now;

        if (deviceId.HasValue)
        {
            await UpsertLastActiveDeviceAccountAsync(
                deviceId.Value,
                user.Id,
                session.IsOfflineRestricted,
                now,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SignInResponseDto(
            MapUser(user),
            MapSession(session),
            accessToken,
            refreshToken);
    }

    private async Task UpsertLastActiveDeviceAccountAsync(
        Guid deviceId,
        Guid userId,
        bool isOfflineRestricted,
        DateTimeOffset markedAtUtc,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.AuthLastActiveDeviceAccounts
            .SingleOrDefaultAsync(
                item => item.DeviceId == deviceId,
                cancellationToken);

        if (record is null)
        {
            dbContext.AuthLastActiveDeviceAccounts.Add(new AuthLastActiveDeviceAccount
            {
                DeviceId = deviceId,
                UserId = userId,
                IsOfflineRestricted = isOfflineRestricted,
                MarkedAtUtc = markedAtUtc
            });

            return;
        }

        record.UserId = userId;
        record.IsOfflineRestricted = isOfflineRestricted;
        record.MarkedAtUtc = markedAtUtc;
    }

    private static CurrentUserSummaryDto MapUser(AuthUser user)
    {
        var roles = user.UserRoles
            .Select(item => item.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToArray();

        var permissions = user.UserRoles
            .SelectMany(item => item.Role.RolePermissions)
            .Select(item => item.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToArray();

        return new CurrentUserSummaryDto(
            user.Id,
            user.Login,
            user.DisplayName,
            roles,
            permissions,
            user.CurrentGroupNodeId);
    }

    private static SessionInfoDto MapSession(AuthSession session)
    {
        return new SessionInfoDto(
            session.Id,
            session.UserId,
            session.DeviceId,
            session.IsOfflineRestricted,
            session.IssuedAtUtc,
            session.ExpiresAtUtc);
    }

    private static string NormalizeLogin(string login)
    {
        return login.Trim().ToUpperInvariant();
    }

    private static string CreateOpaqueToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string ComputeHash(string value)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));
    }
}