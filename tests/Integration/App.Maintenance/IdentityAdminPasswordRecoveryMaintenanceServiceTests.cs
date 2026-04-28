using App.Maintenance.IdentityAdmin;
using App.Maintenance.IntegrationAccounts;

using BuildingBlocks.Infrastructure.Observability;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Audit;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Xunit;

namespace App.Maintenance.Tests;

public sealed class IdentityAdminPasswordRecoveryMaintenanceServiceTests
{
    [Fact]
    public async Task ResetPasswordAsyncResetsExistingInteractiveRootPlatformAdminPasswordAndWritesAudit()
    {
        const string login = "incident-routing-admin";
        var oldPassword = CreateEphemeralSecret();
        var newPassword = CreateEphemeralSecret();

        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login,
            password: newPassword);

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var seed = await SeedInteractiveRootAdminAsync(dbContext, passwordHasher, login, oldPassword);
        var oldHash = seed.User.PasswordHash;
        var otherUser = await SeedInteractiveUserAsync(
            dbContext,
            passwordHasher,
            "branch-admin",
            CreateEphemeralSecret(),
            seed.RootNode.Id);
        var targetActiveSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "target-active-access-hash",
            "target-active-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var targetRefreshOnlySession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "target-refresh-only-access-hash",
            "target-refresh-only-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var targetExpiredSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "target-expired-access-hash",
            "target-expired-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        var alreadyRevokedAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        var targetAlreadyRevokedSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "target-revoked-access-hash",
            "target-revoked-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12),
            revokedAtUtc: alreadyRevokedAtUtc);
        var otherActiveSession = await AddAuthSessionAsync(
            dbContext,
            otherUser.Id,
            "other-active-access-hash",
            "other-active-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        var result = await service.ResetPasswordAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleAsync(item => item.NormalizedLogin == "INCIDENT-ROUTING-ADMIN");
        var auditRecords = await dbContext.AuditRecords
            .OrderBy(item => item.Action)
            .ToArrayAsync();

        Assert.Equal(IdentityAdminPasswordRecoveryStatus.PasswordReset, result.Status);
        Assert.Equal(login, result.Login);
        Assert.Equal(IdentityAdminPasswordRecoveryMaintenanceService.PlatformOwnerRoleCode, result.AssignedRoleCode);
        Assert.Equal(IdentityAdminPasswordRecoveryMaintenanceService.RootGroupNodeCode, result.AssignedGroupNodeCode);
        Assert.Equal(2, result.SessionsRevoked);
        Assert.Equal("admin_password_recovery_succeeded", result.AuditAction);
        Assert.NotEqual(oldHash, user.PasswordHash);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, newPassword));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword));
        Assert.Single(user.UserRoles);
        Assert.Equal(seed.Role.Id, user.UserRoles.Single().RoleId);
        Assert.True(await dbContext.GroupAdminAssignments.AnyAsync(
            item => item.GroupNodeId == seed.RootNode.Id && item.UserId == user.Id));
        Assert.Equal(2, auditRecords.Length);
        Assert.Contains(auditRecords, item => item.Action == "admin_password_recovery_attempted");
        Assert.Contains(auditRecords, item =>
            item.Action == "admin_password_recovery_succeeded"
            && item.PayloadJson!.Contains("\"sessionsRevoked\":2", StringComparison.Ordinal));
        Assert.All(auditRecords, item => Assert.Equal(AuditCategories.Authentication, item.Category));
        Assert.All(auditRecords, item => Assert.Equal("App.Maintenance identity-admin reset-password", item.RequestPath));
        await AssertSessionRevocationAsync(
            dbContext,
            targetActiveSession.Id,
            expectedRevoked: true);
        await AssertSessionRevocationAsync(
            dbContext,
            targetRefreshOnlySession.Id,
            expectedRevoked: true);
        await AssertSessionRevocationAsync(
            dbContext,
            targetExpiredSession.Id,
            expectedRevoked: false);
        await AssertSessionRevocationAsync(
            dbContext,
            targetAlreadyRevokedSession.Id,
            alreadyRevokedAtUtc);
        await AssertSessionRevocationAsync(
            dbContext,
            otherActiveSession.Id,
            expectedRevoked: false);
        AssertAuditRecordsAreSecretSafe(
            auditRecords,
            oldPassword,
            newPassword,
            oldHash,
            user.PasswordHash,
            targetActiveSession.AccessTokenHash,
            targetActiveSession.RefreshTokenHash,
            targetRefreshOnlySession.AccessTokenHash,
            targetRefreshOnlySession.RefreshTokenHash);
    }

    [Fact]
    public async Task ResetPasswordAsyncThrowsWhenRecoveryIsDisabled()
    {
        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: null,
            login: "incident-routing-admin",
            password: CreateEphemeralSecret());

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var seed = await SeedInteractiveRootAdminAsync(
            dbContext,
            passwordHasher,
            "incident-routing-admin",
            CreateEphemeralSecret());
        var oldHash = seed.User.PasswordHash;
        var activeSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "disabled-active-access-hash",
            "disabled-active-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        dbContext.ChangeTracker.Clear();
        var user = await dbContext.AuthUsers.SingleAsync();

        Assert.Equal(
            $"Environment variable {IdentityAdminPasswordRecoveryMaintenanceService.EnabledEnvironmentVariableName} must be set to 'true' to run admin password recovery.",
            exception.Message);
        Assert.Equal(oldHash, user.PasswordHash);
        await AssertSessionRevocationAsync(
            dbContext,
            activeSession.Id,
            expectedRevoked: false);
        Assert.False(await dbContext.AuditRecords.AnyAsync());
    }

    [Fact]
    public async Task ResetPasswordAsyncThrowsWhenLoginEnvironmentVariableIsMissing()
    {
        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login: null,
            password: CreateEphemeralSecret());

        using var dbContext = CreateDbContext();
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        Assert.Equal(
            $"Environment variable {IdentityAdminPasswordRecoveryMaintenanceService.LoginEnvironmentVariableName} is required for Admin login.",
            exception.Message);
        Assert.False(await dbContext.AuditRecords.AnyAsync());
    }

    [Fact]
    public async Task ResetPasswordAsyncThrowsWhenPasswordEnvironmentVariableIsMissing()
    {
        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login: "incident-routing-admin",
            password: null);

        using var dbContext = CreateDbContext();
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        Assert.Equal(
            $"Environment variable {IdentityAdminPasswordRecoveryMaintenanceService.PasswordEnvironmentVariableName} is required. The tool does not accept command-line passwords.",
            exception.Message);
        Assert.False(await dbContext.AuditRecords.AnyAsync());
    }

    [Fact]
    public async Task ResetPasswordAsyncRejectsNonexistentAccountWithSafeAudit()
    {
        var password = CreateEphemeralSecret();

        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login: "missing-admin",
            password);

        using var dbContext = CreateDbContext();
        await SeedRoleAndRootAsync(dbContext);
        var rootNodeId = await dbContext.GroupNodes
            .Where(item => item.Code == IdentityAdminPasswordRecoveryMaintenanceService.RootGroupNodeCode)
            .Select(item => item.Id)
            .SingleAsync();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var otherUser = await SeedInteractiveUserAsync(
            dbContext,
            passwordHasher,
            "other-admin",
            CreateEphemeralSecret(),
            rootNodeId);
        var otherActiveSession = await AddAuthSessionAsync(
            dbContext,
            otherUser.Id,
            "missing-target-other-access-hash",
            "missing-target-other-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(
            dbContext,
            passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        var auditRecords = await dbContext.AuditRecords.ToArrayAsync();

        Assert.Equal(
            "Interactive root/platform admin 'missing-admin' was not found. Password was not changed.",
            exception.Message);
        Assert.Equal(1, await dbContext.AuthUsers.CountAsync());
        await AssertSessionRevocationAsync(
            dbContext,
            otherActiveSession.Id,
            expectedRevoked: false);
        Assert.Equal(2, auditRecords.Length);
        Assert.Contains(auditRecords, item => item.Action == "admin_password_recovery_attempted");
        Assert.Contains(auditRecords, item =>
            item.Action == "admin_password_recovery_rejected"
            && item.PayloadJson!.Contains("target_admin_not_found", StringComparison.Ordinal));
        AssertAuditRecordsAreSecretSafe(auditRecords, password);
    }

    [Fact]
    public async Task ResetPasswordAsyncRejectsKnownIntegrationAccount()
    {
        var oldPassword = CreateEphemeralSecret();
        var newPassword = CreateEphemeralSecret();

        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login: IntegrationAccountMaintenanceService.IntegrationAccountLogin,
            password: newPassword);

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var seed = await SeedInteractiveRootAdminAsync(
            dbContext,
            passwordHasher,
            IntegrationAccountMaintenanceService.IntegrationAccountLogin,
            oldPassword);
        seed.User.DisplayName = IntegrationAccountMaintenanceService.IntegrationAccountDisplayName;
        await dbContext.SaveChangesAsync();
        var oldHash = seed.User.PasswordHash;
        var activeSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "integration-account-access-hash",
            "integration-account-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        dbContext.ChangeTracker.Clear();
        var user = await dbContext.AuthUsers.SingleAsync();
        var auditRecords = await dbContext.AuditRecords.ToArrayAsync();

        Assert.Equal(
            $"Target account '{IntegrationAccountMaintenanceService.IntegrationAccountLogin}' is not eligible for admin password recovery.",
            exception.Message);
        Assert.Equal(oldHash, user.PasswordHash);
        await AssertSessionRevocationAsync(
            dbContext,
            activeSession.Id,
            expectedRevoked: false);
        Assert.Equal(2, auditRecords.Length);
        Assert.Contains(auditRecords, item =>
            item.Action == "admin_password_recovery_rejected"
            && item.PayloadJson!.Contains("target_is_non_interactive_account", StringComparison.Ordinal));
        AssertAuditRecordsAreSecretSafe(auditRecords, oldPassword, newPassword, oldHash);
    }

    [Fact]
    public async Task ResetPasswordAsyncRejectsInactiveAccountWithoutRevokingSessions()
    {
        const string login = "incident-routing-admin";
        var oldPassword = CreateEphemeralSecret();
        var newPassword = CreateEphemeralSecret();

        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login,
            password: newPassword);

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var seed = await SeedInteractiveRootAdminAsync(dbContext, passwordHasher, login, oldPassword);
        seed.User.IsActive = false;
        await dbContext.SaveChangesAsync();
        var oldHash = seed.User.PasswordHash;
        var activeSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            "inactive-account-access-hash",
            "inactive-account-refresh-hash",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        dbContext.ChangeTracker.Clear();
        var user = await dbContext.AuthUsers.SingleAsync();
        var auditRecords = await dbContext.AuditRecords.ToArrayAsync();

        Assert.Equal(
            "Target account 'incident-routing-admin' is not an active root/platform admin. Password was not changed.",
            exception.Message);
        Assert.Equal(oldHash, user.PasswordHash);
        await AssertSessionRevocationAsync(
            dbContext,
            activeSession.Id,
            expectedRevoked: false);
        Assert.Equal(2, auditRecords.Length);
        Assert.Contains(auditRecords, item =>
            item.Action == "admin_password_recovery_rejected"
            && item.PayloadJson!.Contains("target_admin_inactive", StringComparison.Ordinal));
        AssertAuditRecordsAreSecretSafe(auditRecords, oldPassword, newPassword, oldHash);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ResetPasswordAsyncRejectsAccountsWithoutRootPlatformAdminPolicy(
        bool hasPlatformOwnerRole,
        bool hasRootGroupAdminAssignment)
    {
        const string login = "branch-admin";
        var oldPassword = CreateEphemeralSecret();
        var newPassword = CreateEphemeralSecret();

        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login,
            password: newPassword);

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var seed = await SeedInteractiveRootAdminAsync(dbContext, passwordHasher, login, oldPassword);
        if (!hasPlatformOwnerRole)
        {
            seed.User.UserRoles.Clear();
        }

        if (!hasRootGroupAdminAssignment)
        {
            dbContext.GroupAdminAssignments.RemoveRange(dbContext.GroupAdminAssignments);
        }

        await dbContext.SaveChangesAsync();
        var oldHash = seed.User.PasswordHash;
        var activeSession = await AddAuthSessionAsync(
            dbContext,
            seed.User.Id,
            $"policy-validation-access-hash-{hasPlatformOwnerRole}-{hasRootGroupAdminAssignment}",
            $"policy-validation-refresh-hash-{hasPlatformOwnerRole}-{hasRootGroupAdminAssignment}",
            expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        dbContext.ChangeTracker.Clear();
        var user = await dbContext.AuthUsers.SingleAsync();
        var auditRecords = await dbContext.AuditRecords.ToArrayAsync();

        Assert.Equal(
            "Target account 'branch-admin' is not an active root/platform admin. Password was not changed.",
            exception.Message);
        Assert.Equal(oldHash, user.PasswordHash);
        await AssertSessionRevocationAsync(
            dbContext,
            activeSession.Id,
            expectedRevoked: false);
        Assert.Equal(2, auditRecords.Length);
        Assert.Contains(auditRecords, item =>
            item.Action == "admin_password_recovery_rejected"
            && item.PayloadJson!.Contains("target_not_root_platform_admin", StringComparison.Ordinal));
        AssertAuditRecordsAreSecretSafe(auditRecords, oldPassword, newPassword, oldHash);
    }

    [Fact]
    public async Task ResetPasswordAsyncIsIdempotentForExistingRootPlatformAdmin()
    {
        const string login = "incident-routing-admin";
        var oldPassword = CreateEphemeralSecret();
        var firstPassword = CreateEphemeralSecret();
        var secondPassword = CreateEphemeralSecret();

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        await SeedInteractiveRootAdminAsync(dbContext, passwordHasher, login, oldPassword);
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        using (new IdentityAdminPasswordRecoveryEnvironment("true", login, firstPassword))
        {
            _ = await service.ResetPasswordAsync(CancellationToken.None);
        }

        using (new IdentityAdminPasswordRecoveryEnvironment("true", login, secondPassword))
        {
            _ = await service.ResetPasswordAsync(CancellationToken.None);
        }

        dbContext.ChangeTracker.Clear();
        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleAsync(item => item.NormalizedLogin == "INCIDENT-ROUTING-ADMIN");

        Assert.Equal(1, await dbContext.AuthUsers.CountAsync());
        Assert.Equal(1, await dbContext.AuthUserRoles.CountAsync());
        Assert.Equal(1, await dbContext.GroupAdminAssignments.CountAsync());
        Assert.Equal(4, await dbContext.AuditRecords.CountAsync());
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, secondPassword));
        Assert.Equal(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, firstPassword));
    }

    [Fact]
    public async Task ResetPasswordAsyncDoesNotChangePasswordWhenRootPolicyValidationFails()
    {
        const string login = "incident-routing-admin";
        var oldPassword = CreateEphemeralSecret();
        var newPassword = CreateEphemeralSecret();

        using var environment = new IdentityAdminPasswordRecoveryEnvironment(
            enabled: "true",
            login,
            password: newPassword);

        using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasher<AuthUser>();
        var role = new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = IdentityAdminPasswordRecoveryMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        };
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = login,
            NormalizedLogin = "INCIDENT-ROUTING-ADMIN",
            DisplayName = "Incident Routing Admin",
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
        user.PasswordHash = passwordHasher.HashPassword(user, oldPassword);
        user.UserRoles.Add(new AuthUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        dbContext.AuthRoles.Add(role);
        dbContext.AuthUsers.Add(user);
        await dbContext.SaveChangesAsync();
        var oldHash = user.PasswordHash;
        var service = new IdentityAdminPasswordRecoveryMaintenanceService(dbContext, passwordHasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetPasswordAsync(CancellationToken.None));

        dbContext.ChangeTracker.Clear();
        var unchangedUser = await dbContext.AuthUsers.SingleAsync();

        Assert.Equal(
            $"Active group node '{IdentityAdminPasswordRecoveryMaintenanceService.RootGroupNodeCode}' was not found. Password was not changed.",
            exception.Message);
        Assert.Equal(oldHash, unchangedUser.PasswordHash);
        Assert.Equal(2, await dbContext.AuditRecords.CountAsync());
    }

    private static async Task<(AuthRole Role, GroupNode RootNode, AuthUser User)> SeedInteractiveRootAdminAsync(
        PlatformDbContext dbContext,
        PasswordHasher<AuthUser> passwordHasher,
        string login,
        string password)
    {
        var role = new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = IdentityAdminPasswordRecoveryMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        };
        var rootNode = new GroupNode
        {
            Id = Guid.NewGuid(),
            Code = IdentityAdminPasswordRecoveryMaintenanceService.RootGroupNodeCode,
            Name = "Root",
            Depth = 0,
            IsActive = true
        };
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = login,
            NormalizedLogin = login.Trim().ToUpperInvariant(),
            DisplayName = login,
            IsActive = true,
            CurrentGroupNodeId = rootNode.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        user.UserRoles.Add(new AuthUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        dbContext.AuthRoles.Add(role);
        dbContext.GroupNodes.Add(rootNode);
        dbContext.AuthUsers.Add(user);
        dbContext.GroupAdminAssignments.Add(new GroupAdminAssignment
        {
            GroupNodeId = rootNode.Id,
            UserId = user.Id,
            AssignedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        return (role, rootNode, user);
    }

    private static async Task<AuthUser> SeedInteractiveUserAsync(
        PlatformDbContext dbContext,
        PasswordHasher<AuthUser> passwordHasher,
        string login,
        string password,
        Guid? currentGroupNodeId)
    {
        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = login,
            NormalizedLogin = login.Trim().ToUpperInvariant(),
            DisplayName = login,
            IsActive = true,
            CurrentGroupNodeId = currentGroupNodeId,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.AuthUsers.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    private static async Task<AuthSession> AddAuthSessionAsync(
        PlatformDbContext dbContext,
        Guid userId,
        string accessTokenHash,
        string refreshTokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset refreshExpiresAtUtc,
        DateTimeOffset? revokedAtUtc = null)
    {
        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = Guid.NewGuid(),
            AccessTokenHash = accessTokenHash,
            RefreshTokenHash = refreshTokenHash,
            IsOfflineRestricted = false,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = expiresAtUtc,
            RefreshExpiresAtUtc = refreshExpiresAtUtc,
            RevokedAtUtc = revokedAtUtc
        };

        dbContext.AuthSessions.Add(session);
        await dbContext.SaveChangesAsync();

        return session;
    }

    private static async Task AssertSessionRevocationAsync(
        PlatformDbContext dbContext,
        Guid sessionId,
        bool expectedRevoked)
    {
        var revokedAtUtc = await dbContext.AuthSessions
            .AsNoTracking()
            .Where(item => item.Id == sessionId)
            .Select(item => item.RevokedAtUtc)
            .SingleAsync();

        if (expectedRevoked)
        {
            Assert.NotNull(revokedAtUtc);
            return;
        }

        Assert.Null(revokedAtUtc);
    }

    private static async Task AssertSessionRevocationAsync(
        PlatformDbContext dbContext,
        Guid sessionId,
        DateTimeOffset expectedRevokedAtUtc)
    {
        var revokedAtUtc = await dbContext.AuthSessions
            .AsNoTracking()
            .Where(item => item.Id == sessionId)
            .Select(item => item.RevokedAtUtc)
            .SingleAsync();

        Assert.Equal(expectedRevokedAtUtc, revokedAtUtc);
    }

    private static async Task SeedRoleAndRootAsync(PlatformDbContext dbContext)
    {
        dbContext.AuthRoles.Add(new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = IdentityAdminPasswordRecoveryMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        });
        dbContext.GroupNodes.Add(new GroupNode
        {
            Id = Guid.NewGuid(),
            Code = IdentityAdminPasswordRecoveryMaintenanceService.RootGroupNodeCode,
            Name = "Root",
            Depth = 0,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
    }

    private static PlatformDbContext CreateDbContext()
    {
        return CreateDbContext(
            $"identity-admin-password-recovery-tests-{Guid.NewGuid():N}",
            new InMemoryDatabaseRoot());
    }

    private static PlatformDbContext CreateDbContext(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        return new PlatformDbContext(options);
    }

    private static string CreateEphemeralSecret()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    }

    private static void AssertAuditRecordsAreSecretSafe(
        IEnumerable<AuditRecord> auditRecords,
        params string[] secrets)
    {
        foreach (var auditRecord in auditRecords)
        {
            var payload = auditRecord.PayloadJson ?? string.Empty;
            Assert.DoesNotContain("accessToken", payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refreshToken", payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authorization", payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("connection string", payload, StringComparison.OrdinalIgnoreCase);

            foreach (var secret in secrets)
            {
                Assert.DoesNotContain(secret, payload, StringComparison.Ordinal);
            }
        }
    }

    private sealed class IdentityAdminPasswordRecoveryEnvironment : IDisposable
    {
        private readonly EnvironmentVariableScope enabled;
        private readonly EnvironmentVariableScope login;
        private readonly EnvironmentVariableScope password;

        public IdentityAdminPasswordRecoveryEnvironment(
            string? enabled,
            string? login,
            string? password)
        {
            this.enabled = new EnvironmentVariableScope(
                IdentityAdminPasswordRecoveryMaintenanceService.EnabledEnvironmentVariableName,
                enabled);
            this.login = new EnvironmentVariableScope(
                IdentityAdminPasswordRecoveryMaintenanceService.LoginEnvironmentVariableName,
                login);
            this.password = new EnvironmentVariableScope(
                IdentityAdminPasswordRecoveryMaintenanceService.PasswordEnvironmentVariableName,
                password);
        }

        public void Dispose()
        {
            password.Dispose();
            login.Dispose();
            enabled.Dispose();
        }
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string name;
        private readonly string? originalValue;

        public EnvironmentVariableScope(string name, string? value)
        {
            this.name = name;
            originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(name, originalValue);
        }
    }
}