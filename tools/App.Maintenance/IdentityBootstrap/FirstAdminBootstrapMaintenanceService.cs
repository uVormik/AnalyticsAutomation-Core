using System.Text.Json;

using BuildingBlocks.Infrastructure.Observability;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Audit;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Maintenance.IdentityBootstrap;

public sealed class FirstAdminBootstrapMaintenanceService(
    PlatformDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher)
{
    public const string EnabledEnvironmentVariableName = "AA_FIRST_ADMIN_BOOTSTRAP_ENABLED";
    public const string LoginEnvironmentVariableName = "AA_FIRST_ADMIN_LOGIN";
    public const string PasswordEnvironmentVariableName = "AA_FIRST_ADMIN_PASSWORD";
    public const string DisplayNameEnvironmentVariableName = "AA_FIRST_ADMIN_DISPLAY_NAME";
    public const string PlatformOwnerRoleCode = "platform_owner";
    public const string RootGroupNodeCode = "root";

    private const string MaintenanceRequestPath = "App.Maintenance identity-bootstrap first-admin";

    public async Task<FirstAdminBootstrapResult> BootstrapAsync(CancellationToken cancellationToken)
    {
        EnsureExplicitlyEnabled();

        var login = ReadRequiredText(LoginEnvironmentVariableName, "First admin login", 128);
        var password = ReadRequiredPassword();
        var displayName = ReadOptionalText(DisplayNameEnvironmentVariableName, 256) ?? login;

        var role = await dbContext.AuthRoles
            .SingleOrDefaultAsync(
                item => item.Code == PlatformOwnerRoleCode,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Role '{PlatformOwnerRoleCode}' was not found. Aborting without changes.");

        var rootGroupNode = await dbContext.GroupNodes
            .SingleOrDefaultAsync(
                item => item.Code == RootGroupNodeCode && item.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Active group node '{RootGroupNodeCode}' was not found. Aborting without changes.");

        var existingActiveAdmin = await dbContext.AuthUsers
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Where(item => item.UserRoles.Any(roleLink => roleLink.RoleId == role.Id))
            .OrderBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingActiveAdmin is not null)
        {
            await WriteAuditRecordAsync(
                "first_admin_bootstrap_skipped_existing_admin",
                existingActiveAdmin.Id,
                existingActiveAdmin.Login,
                role.Code,
                rootGroupNode.Code,
                wasCreated: false,
                wasRoleLinkCreated: false,
                wasAssignmentCreated: false,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            return new FirstAdminBootstrapResult(
                existingActiveAdmin.Login,
                FirstAdminBootstrapStatus.SkippedExistingAdmin,
                role.Code,
                WasRoleLinkCreated: false,
                rootGroupNode.Code,
                WasAssignmentCreated: false,
                "first_admin_bootstrap_skipped_existing_admin");
        }

        var normalizedLogin = NormalizeLogin(login);
        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleOrDefaultAsync(
                item => item.NormalizedLogin == normalizedLogin,
                cancellationToken);

        var wasCreated = user is null;
        if (user is null)
        {
            user = new AuthUser
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            dbContext.AuthUsers.Add(user);
        }

        user.Login = login;
        user.NormalizedLogin = normalizedLogin;
        user.DisplayName = displayName;
        user.IsActive = true;
        user.CurrentGroupNodeId = rootGroupNode.Id;
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        var wasRoleLinkCreated = false;
        if (!user.UserRoles.Any(item => item.RoleId == role.Id))
        {
            user.UserRoles.Add(new AuthUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            wasRoleLinkCreated = true;
        }

        var assignment = await dbContext.GroupAdminAssignments
            .SingleOrDefaultAsync(
                item => item.GroupNodeId == rootGroupNode.Id && item.UserId == user.Id,
                cancellationToken);

        var wasAssignmentCreated = assignment is null;
        if (assignment is null)
        {
            dbContext.GroupAdminAssignments.Add(new GroupAdminAssignment
            {
                GroupNodeId = rootGroupNode.Id,
                UserId = user.Id,
                AssignedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await WriteAuditRecordAsync(
            "first_admin_bootstrapped",
            user.Id,
            user.Login,
            role.Code,
            rootGroupNode.Code,
            wasCreated,
            wasRoleLinkCreated,
            wasAssignmentCreated,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new FirstAdminBootstrapResult(
            user.Login,
            wasCreated ? FirstAdminBootstrapStatus.Created : FirstAdminBootstrapStatus.Updated,
            role.Code,
            wasRoleLinkCreated,
            rootGroupNode.Code,
            wasAssignmentCreated,
            "first_admin_bootstrapped");
    }

    private Task WriteAuditRecordAsync(
        string action,
        Guid subjectUserId,
        string login,
        string roleCode,
        string groupNodeCode,
        bool wasCreated,
        bool wasRoleLinkCreated,
        bool wasAssignmentCreated,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = new
        {
            login,
            roleCode,
            groupNodeCode,
            wasCreated,
            wasRoleLinkCreated,
            wasAssignmentCreated
        };

        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            Category = AuditCategories.Authentication,
            Action = action,
            SubjectUserId = subjectUserId,
            EntityType = "auth_user",
            EntityId = subjectUserId.ToString("D"),
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
                $"Environment variable {EnabledEnvironmentVariableName} must be set to 'true' to run first-admin bootstrap.");
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
        var value = ReadOptionalText(environmentVariableName, maxLength);
        if (value is null)
        {
            throw new InvalidOperationException(
                $"Environment variable {environmentVariableName} is required for {valueName}.");
        }

        return value;
    }

    private static string? ReadOptionalText(
        string environmentVariableName,
        int maxLength)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
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

    private static string NormalizeLogin(string login)
    {
        return login.Trim().ToUpperInvariant();
    }
}

public enum FirstAdminBootstrapStatus
{
    Created,
    Updated,
    SkippedExistingAdmin
}

public sealed record FirstAdminBootstrapResult(
    string Login,
    FirstAdminBootstrapStatus Status,
    string AssignedRoleCode,
    bool WasRoleLinkCreated,
    string AssignedGroupNodeCode,
    bool WasAssignmentCreated,
    string AuditAction);