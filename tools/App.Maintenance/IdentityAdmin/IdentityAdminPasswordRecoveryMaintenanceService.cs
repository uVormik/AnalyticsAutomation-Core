using System.Data;
using System.Text.Json;

using App.Maintenance.IntegrationAccounts;

using BuildingBlocks.Infrastructure.Observability;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Audit;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.Maintenance.IdentityAdmin;

public sealed class IdentityAdminPasswordRecoveryMaintenanceService(
    PlatformDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher)
{
    public const string EnabledEnvironmentVariableName = "AA_ADMIN_PASSWORD_RECOVERY_ENABLED";
    public const string LoginEnvironmentVariableName = "AA_ADMIN_PASSWORD_RECOVERY_LOGIN";
    public const string PasswordEnvironmentVariableName = "AA_ADMIN_PASSWORD_RECOVERY_PASSWORD";
    public const string PlatformOwnerRoleCode = "platform_owner";
    public const string RootGroupNodeCode = "root";

    private const string MaintenanceMode = "reset-password";
    private const string MaintenanceRequestPath = "App.Maintenance identity-admin reset-password";

    public async Task<IdentityAdminPasswordRecoveryResult> ResetPasswordAsync(
        CancellationToken cancellationToken)
    {
        EnsureExplicitlyEnabled();

        var login = ReadRequiredText(LoginEnvironmentVariableName, "Admin login", 128);
        var password = ReadRequiredPassword();

        await using var transaction = await BeginSerializableTransactionIfSupportedAsync(cancellationToken);

        await WriteAuditRecordAsync(
            "admin_password_recovery_attempted",
            subjectUserId: null,
            login,
            outcome: "attempted",
            reason: null,
            roleCode: null,
            groupNodeCode: null,
            hasPlatformOwnerRole: null,
            hasRootGroupAdminAssignment: null,
            cancellationToken);

        var role = await dbContext.AuthRoles
            .SingleOrDefaultAsync(
                item => item.Code == PlatformOwnerRoleCode,
                cancellationToken);

        if (role is null)
        {
            await FailWithAuditAsync(
                login,
                subjectUserId: null,
                outcome: "failed",
                reason: "platform_owner_role_missing",
                roleCode: PlatformOwnerRoleCode,
                groupNodeCode: null,
                hasPlatformOwnerRole: false,
                hasRootGroupAdminAssignment: null,
                transaction,
                cancellationToken);

            throw new InvalidOperationException(
                $"Role '{PlatformOwnerRoleCode}' was not found. Password was not changed.");
        }

        var rootGroupNode = await dbContext.GroupNodes
            .SingleOrDefaultAsync(
                item => item.Code == RootGroupNodeCode && item.IsActive,
                cancellationToken);

        if (rootGroupNode is null)
        {
            await FailWithAuditAsync(
                login,
                subjectUserId: null,
                outcome: "failed",
                reason: "active_root_group_missing",
                role.Code,
                groupNodeCode: RootGroupNodeCode,
                hasPlatformOwnerRole: null,
                hasRootGroupAdminAssignment: false,
                transaction,
                cancellationToken);

            throw new InvalidOperationException(
                $"Active group node '{RootGroupNodeCode}' was not found. Password was not changed.");
        }

        var normalizedLogin = NormalizeLogin(login);
        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleOrDefaultAsync(
                item => item.NormalizedLogin == normalizedLogin,
                cancellationToken);

        if (user is null)
        {
            await FailWithAuditAsync(
                login,
                subjectUserId: null,
                outcome: "not_found",
                reason: "target_admin_not_found",
                role.Code,
                rootGroupNode.Code,
                hasPlatformOwnerRole: null,
                hasRootGroupAdminAssignment: null,
                transaction,
                cancellationToken);

            throw new InvalidOperationException(
                $"Interactive root/platform admin '{login}' was not found. Password was not changed.");
        }

        if (IsKnownNonInteractiveAccount(user))
        {
            await FailWithAuditAsync(
                user.Login,
                user.Id,
                outcome: "rejected",
                reason: "target_is_non_interactive_account",
                role.Code,
                rootGroupNode.Code,
                hasPlatformOwnerRole: user.UserRoles.Any(item => item.RoleId == role.Id),
                hasRootGroupAdminAssignment: await HasRootGroupAdminAssignmentAsync(user.Id, rootGroupNode.Id, cancellationToken),
                transaction,
                cancellationToken);

            throw new InvalidOperationException(
                $"Target account '{user.Login}' is not eligible for admin password recovery.");
        }

        if (!user.IsActive)
        {
            await FailWithAuditAsync(
                user.Login,
                user.Id,
                outcome: "rejected",
                reason: "target_admin_inactive",
                role.Code,
                rootGroupNode.Code,
                hasPlatformOwnerRole: user.UserRoles.Any(item => item.RoleId == role.Id),
                hasRootGroupAdminAssignment: await HasRootGroupAdminAssignmentAsync(user.Id, rootGroupNode.Id, cancellationToken),
                transaction,
                cancellationToken);

            throw new InvalidOperationException(
                $"Target account '{user.Login}' is not an active root/platform admin. Password was not changed.");
        }

        var hasPlatformOwnerRole = user.UserRoles.Any(item => item.RoleId == role.Id);
        var hasRootGroupAdminAssignment = await HasRootGroupAdminAssignmentAsync(
            user.Id,
            rootGroupNode.Id,
            cancellationToken);

        if (!hasPlatformOwnerRole || !hasRootGroupAdminAssignment)
        {
            await FailWithAuditAsync(
                user.Login,
                user.Id,
                outcome: "rejected",
                reason: "target_not_root_platform_admin",
                role.Code,
                rootGroupNode.Code,
                hasPlatformOwnerRole,
                hasRootGroupAdminAssignment,
                transaction,
                cancellationToken);

            throw new InvalidOperationException(
                $"Target account '{user.Login}' is not an active root/platform admin. Password was not changed.");
        }

        var now = DateTimeOffset.UtcNow;
        var sessionsRevoked = await RevokeActiveSessionsAsync(user.Id, now, cancellationToken);

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        await WriteAuditRecordAsync(
            "admin_password_recovery_succeeded",
            user.Id,
            user.Login,
            outcome: "password_reset",
            reason: null,
            role.Code,
            rootGroupNode.Code,
            hasPlatformOwnerRole,
            hasRootGroupAdminAssignment,
            cancellationToken,
            sessionsRevoked);

        await dbContext.SaveChangesAsync(cancellationToken);
        await CommitIfStartedAsync(transaction, cancellationToken);

        return new IdentityAdminPasswordRecoveryResult(
            user.Login,
            IdentityAdminPasswordRecoveryStatus.PasswordReset,
            role.Code,
            rootGroupNode.Code,
            sessionsRevoked,
            "admin_password_recovery_succeeded");
    }

    private async Task FailWithAuditAsync(
        string login,
        Guid? subjectUserId,
        string outcome,
        string reason,
        string? roleCode,
        string? groupNodeCode,
        bool? hasPlatformOwnerRole,
        bool? hasRootGroupAdminAssignment,
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await WriteAuditRecordAsync(
            "admin_password_recovery_rejected",
            subjectUserId,
            login,
            outcome,
            reason,
            roleCode,
            groupNodeCode,
            hasPlatformOwnerRole,
            hasRootGroupAdminAssignment,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await CommitIfStartedAsync(transaction, cancellationToken);
    }

    private async Task<bool> HasRootGroupAdminAssignmentAsync(
        Guid userId,
        Guid rootGroupNodeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.GroupAdminAssignments.AnyAsync(
            item => item.GroupNodeId == rootGroupNodeId && item.UserId == userId,
            cancellationToken);
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionIfSupportedAsync(
        CancellationToken cancellationToken)
    {
        if (string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.InMemory",
            StringComparison.Ordinal))
        {
            return null;
        }

        return await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private static async Task CommitIfStartedAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            return;
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<int> RevokeActiveSessionsAsync(
        Guid userId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var activeSessions = await dbContext.AuthSessions
            .Where(item => item.UserId == userId
                && item.RevokedAtUtc == null
                && (item.ExpiresAtUtc > revokedAtUtc || item.RefreshExpiresAtUtc > revokedAtUtc))
            .ToArrayAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = revokedAtUtc;
        }

        return activeSessions.Length;
    }

    private Task WriteAuditRecordAsync(
        string action,
        Guid? subjectUserId,
        string login,
        string outcome,
        string? reason,
        string? roleCode,
        string? groupNodeCode,
        bool? hasPlatformOwnerRole,
        bool? hasRootGroupAdminAssignment,
        CancellationToken cancellationToken,
        int? sessionsRevoked = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = new
        {
            mode = MaintenanceMode,
            login,
            outcome,
            reason,
            roleCode,
            groupNodeCode,
            hasPlatformOwnerRole,
            hasRootGroupAdminAssignment,
            sessionsRevoked
        };

        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            Category = AuditCategories.Authentication,
            Action = action,
            SubjectUserId = subjectUserId,
            EntityType = "auth_user",
            EntityId = subjectUserId?.ToString("D"),
            RequestPath = MaintenanceRequestPath,
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    private static void EnsureExplicitlyEnabled()
    {
        var value = Environment.GetEnvironmentVariable(EnabledEnvironmentVariableName);

        if (!string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Environment variable {EnabledEnvironmentVariableName} must be set to 'true' to run admin password recovery.");
        }
    }

    private static string ReadRequiredPassword()
    {
        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Environment variable {PasswordEnvironmentVariableName} is required. The tool does not accept command-line passwords.");
        }

        return password;
    }

    private static string ReadRequiredText(
        string environmentVariableName,
        string valueName,
        int maxLength)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Environment variable {environmentVariableName} is required for {valueName}.");
        }

        value = value.Trim();
        if (value.Length > maxLength)
        {
            throw new InvalidOperationException(
                $"Environment variable {environmentVariableName} must be {maxLength} characters or fewer.");
        }

        if (value.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                $"Environment variable {environmentVariableName} must not contain control characters.");
        }

        return value;
    }

    private static bool IsKnownNonInteractiveAccount(AuthUser user)
    {
        return string.Equals(
            user.NormalizedLogin,
            NormalizeLogin(IntegrationAccountMaintenanceService.IntegrationAccountLogin),
            StringComparison.Ordinal);
    }

    private static string NormalizeLogin(string login)
    {
        return login.Trim().ToUpperInvariant();
    }
}

public enum IdentityAdminPasswordRecoveryStatus
{
    PasswordReset
}

public sealed record IdentityAdminPasswordRecoveryResult(
    string Login,
    IdentityAdminPasswordRecoveryStatus Status,
    string AssignedRoleCode,
    string AssignedGroupNodeCode,
    int SessionsRevoked,
    string AuditAction);