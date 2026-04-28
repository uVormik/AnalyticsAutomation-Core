using App.Maintenance.IdentityBootstrap;

using BuildingBlocks.Infrastructure.Observability;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Xunit;

namespace App.Maintenance.Tests;

public sealed class FirstAdminBootstrapMaintenanceServiceTests
{
    [Fact]
    public async Task BootstrapAsyncCreatesFirstAdminWithRootGroupRoleAssignmentAndAudit()
    {
        const string login = "first-admin-smoke";
        const string normalizedLogin = "FIRST-ADMIN-SMOKE";
        const string displayName = "First Admin Smoke";
        var password = CreateEphemeralSecret();

        using var environment = new FirstAdminBootstrapEnvironment(
            enabled: "true",
            login,
            password,
            displayName);

        using var dbContext = CreateDbContext();
        var role = new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = FirstAdminBootstrapMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        };
        var rootNode = new GroupNode
        {
            Id = Guid.NewGuid(),
            Code = FirstAdminBootstrapMaintenanceService.RootGroupNodeCode,
            Name = "Root",
            Depth = 0,
            IsActive = true
        };

        dbContext.AuthRoles.Add(role);
        dbContext.GroupNodes.Add(rootNode);
        await dbContext.SaveChangesAsync();

        var passwordHasher = new PasswordHasher<AuthUser>();
        var service = new FirstAdminBootstrapMaintenanceService(dbContext, passwordHasher);

        var result = await service.BootstrapAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleAsync(item => item.NormalizedLogin == normalizedLogin);
        var assignment = await dbContext.GroupAdminAssignments
            .SingleAsync(item => item.GroupNodeId == rootNode.Id && item.UserId == user.Id);
        var auditRecord = await dbContext.AuditRecords.SingleAsync();

        Assert.Equal(FirstAdminBootstrapStatus.Created, result.Status);
        Assert.Equal(login, result.Login);
        Assert.Equal(FirstAdminBootstrapMaintenanceService.PlatformOwnerRoleCode, result.AssignedRoleCode);
        Assert.True(result.WasRoleLinkCreated);
        Assert.Equal(FirstAdminBootstrapMaintenanceService.RootGroupNodeCode, result.AssignedGroupNodeCode);
        Assert.True(result.WasAssignmentCreated);
        Assert.Equal("first_admin_bootstrapped", result.AuditAction);
        Assert.Equal(login, user.Login);
        Assert.Equal(displayName, user.DisplayName);
        Assert.True(user.IsActive);
        Assert.Equal(rootNode.Id, user.CurrentGroupNodeId);
        Assert.Single(user.UserRoles);
        Assert.Equal(role.Id, user.UserRoles.Single().RoleId);
        Assert.Equal(rootNode.Id, assignment.GroupNodeId);
        Assert.Equal(user.Id, assignment.UserId);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password));
        Assert.Equal(AuditCategories.Authentication, auditRecord.Category);
        Assert.Equal("first_admin_bootstrapped", auditRecord.Action);
        Assert.Equal(user.Id, auditRecord.SubjectUserId);
        Assert.Equal("auth_user", auditRecord.EntityType);
        Assert.Equal(user.Id.ToString("D"), auditRecord.EntityId);
        Assert.Equal("App.Maintenance identity-bootstrap first-admin", auditRecord.RequestPath);
        Assert.Contains(login, auditRecord.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain(password, auditRecord.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain(user.PasswordHash, auditRecord.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", auditRecord.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", auditRecord.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", auditRecord.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BootstrapAsyncSkipsWithoutPasswordChangeWhenActivePlatformOwnerAlreadyExists()
    {
        const string requestedLogin = "requested-first-admin";
        var password = CreateEphemeralSecret();

        using var environment = new FirstAdminBootstrapEnvironment(
            enabled: "true",
            login: requestedLogin,
            password,
            displayName: null);

        using var dbContext = CreateDbContext();
        var role = new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = FirstAdminBootstrapMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        };
        var rootNode = new GroupNode
        {
            Id = Guid.NewGuid(),
            Code = FirstAdminBootstrapMaintenanceService.RootGroupNodeCode,
            Name = "Root",
            Depth = 0,
            IsActive = true
        };
        var existingAdmin = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = "existing-admin",
            NormalizedLogin = "EXISTING-ADMIN",
            DisplayName = "Existing Admin",
            PasswordHash = "old-hash",
            IsActive = true,
            CurrentGroupNodeId = rootNode.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        existingAdmin.UserRoles.Add(new AuthUserRole
        {
            UserId = existingAdmin.Id,
            RoleId = role.Id
        });

        dbContext.AuthRoles.Add(role);
        dbContext.GroupNodes.Add(rootNode);
        dbContext.AuthUsers.Add(existingAdmin);
        await dbContext.SaveChangesAsync();

        var service = new FirstAdminBootstrapMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var result = await service.BootstrapAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleAsync();
        var auditRecord = await dbContext.AuditRecords.SingleAsync();

        Assert.Equal(FirstAdminBootstrapStatus.SkippedExistingAdmin, result.Status);
        Assert.Equal(existingAdmin.Login, result.Login);
        Assert.False(result.WasRoleLinkCreated);
        Assert.False(result.WasAssignmentCreated);
        Assert.Equal("first_admin_bootstrap_skipped_existing_admin", result.AuditAction);
        Assert.Equal("old-hash", user.PasswordHash);
        Assert.Equal(1, await dbContext.AuthUsers.CountAsync());
        Assert.Equal(1, await dbContext.AuthUserRoles.CountAsync());
        Assert.False(await dbContext.GroupAdminAssignments.AnyAsync());
        Assert.Equal("first_admin_bootstrap_skipped_existing_admin", auditRecord.Action);
        Assert.DoesNotContain(password, auditRecord.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", auditRecord.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", auditRecord.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BootstrapAsyncThrowsWhenBootstrapIsDisabled()
    {
        using var environment = new FirstAdminBootstrapEnvironment(
            enabled: null,
            login: "first-admin-smoke",
            password: CreateEphemeralSecret(),
            displayName: null);

        using var dbContext = CreateDbContext();
        var service = new FirstAdminBootstrapMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BootstrapAsync(CancellationToken.None));

        Assert.Equal(
            $"Environment variable {FirstAdminBootstrapMaintenanceService.EnabledEnvironmentVariableName} must be set to 'true' to run first-admin bootstrap.",
            exception.Message);
        Assert.False(await dbContext.AuthUsers.AnyAsync());
        Assert.False(await dbContext.AuditRecords.AnyAsync());
    }

    [Fact]
    public async Task BootstrapAsyncThrowsWhenPasswordEnvironmentVariableIsMissing()
    {
        using var environment = new FirstAdminBootstrapEnvironment(
            enabled: "true",
            login: "first-admin-smoke",
            password: null,
            displayName: null);

        using var dbContext = CreateDbContext();
        var service = new FirstAdminBootstrapMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BootstrapAsync(CancellationToken.None));

        Assert.Equal(
            $"Environment variable {FirstAdminBootstrapMaintenanceService.PasswordEnvironmentVariableName} is required. The tool does not accept command-line passwords.",
            exception.Message);
        Assert.False(await dbContext.AuthUsers.AnyAsync());
        Assert.False(await dbContext.AuditRecords.AnyAsync());
    }

    [Fact]
    public async Task BootstrapAsyncThrowsWhenLoginEnvironmentVariableIsMissing()
    {
        using var environment = new FirstAdminBootstrapEnvironment(
            enabled: "true",
            login: null,
            password: CreateEphemeralSecret(),
            displayName: null);

        using var dbContext = CreateDbContext();
        var service = new FirstAdminBootstrapMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BootstrapAsync(CancellationToken.None));

        Assert.Equal(
            $"Environment variable {FirstAdminBootstrapMaintenanceService.LoginEnvironmentVariableName} is required for First admin login.",
            exception.Message);
        Assert.False(await dbContext.AuthUsers.AnyAsync());
        Assert.False(await dbContext.AuditRecords.AnyAsync());
    }

    private static PlatformDbContext CreateDbContext()
    {
        return CreateDbContext(
            $"first-admin-bootstrap-tests-{Guid.NewGuid():N}",
            new InMemoryDatabaseRoot());
    }

    private static string CreateEphemeralSecret()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
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

    private sealed class FirstAdminBootstrapEnvironment : IDisposable
    {
        private readonly EnvironmentVariableScope enabled;
        private readonly EnvironmentVariableScope login;
        private readonly EnvironmentVariableScope password;
        private readonly EnvironmentVariableScope displayName;

        public FirstAdminBootstrapEnvironment(
            string? enabled,
            string? login,
            string? password,
            string? displayName)
        {
            this.enabled = new EnvironmentVariableScope(
                FirstAdminBootstrapMaintenanceService.EnabledEnvironmentVariableName,
                enabled);
            this.login = new EnvironmentVariableScope(
                FirstAdminBootstrapMaintenanceService.LoginEnvironmentVariableName,
                login);
            this.password = new EnvironmentVariableScope(
                FirstAdminBootstrapMaintenanceService.PasswordEnvironmentVariableName,
                password);
            this.displayName = new EnvironmentVariableScope(
                FirstAdminBootstrapMaintenanceService.DisplayNameEnvironmentVariableName,
                displayName);
        }

        public void Dispose()
        {
            displayName.Dispose();
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