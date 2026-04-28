using App.Maintenance.IncidentRoutingAdmins;
using App.Maintenance.IdentityAdmin;
using App.Maintenance.IdentityBootstrap;
using App.Maintenance.IntegrationAccounts;

using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Xunit;

namespace App.Maintenance.Tests;

public sealed class AppMaintenanceProgramTests
{
    [Fact]
    public async Task RunAsyncIdentityAdminResetPasswordWritesSafeOutputOnly()
    {
        const string login = "incident-routing-admin";
        var oldPassword = CreateEphemeralSecret();
        var newPassword = CreateEphemeralSecret();

        using var recoveryEnabledScope = new EnvironmentVariableScope(
            IdentityAdminPasswordRecoveryMaintenanceService.EnabledEnvironmentVariableName,
            "true");
        using var loginScope = new EnvironmentVariableScope(
            IdentityAdminPasswordRecoveryMaintenanceService.LoginEnvironmentVariableName,
            login);
        using var passwordScope = new EnvironmentVariableScope(
            IdentityAdminPasswordRecoveryMaintenanceService.PasswordEnvironmentVariableName,
            newPassword);

        var databaseName = $"app-maintenance-program-admin-recovery-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();
        string oldHash;
        string sessionAccessTokenHash;
        string sessionRefreshTokenHash;

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            var passwordHasher = new PasswordHasher<AuthUser>();
            var seed = await SeedInteractiveRootAdminAsync(
                setupContext,
                passwordHasher,
                login,
                oldPassword);
            oldHash = seed.PasswordHash;
            sessionAccessTokenHash = "program-reset-active-access-hash";
            sessionRefreshTokenHash = "program-reset-active-refresh-hash";
            await AddAuthSessionAsync(
                setupContext,
                seed.Id,
                sessionAccessTokenHash,
                sessionRefreshTokenHash,
                expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
                refreshExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12));
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await AppMaintenanceProgram.RunAsync(
            ["identity-admin", "reset-password"],
            () => CreateHost(databaseName, databaseRoot),
            stdout,
            stderr,
            CancellationToken.None);

        await using var assertContext = CreateDbContext(databaseName, databaseRoot);
        var user = await assertContext.AuthUsers
            .SingleAsync(item => item.NormalizedLogin == "INCIDENT-ROUTING-ADMIN");
        var session = await assertContext.AuthSessions.SingleAsync();

        var standardOutput = stdout.ToString();
        var errorOutput = stderr.ToString();

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, errorOutput);
        Assert.Contains("login: incident-routing-admin", standardOutput, StringComparison.Ordinal);
        Assert.Contains("mode: reset-password", standardOutput, StringComparison.Ordinal);
        Assert.Contains("status: password_reset", standardOutput, StringComparison.Ordinal);
        Assert.Contains("role: platform_owner", standardOutput, StringComparison.Ordinal);
        Assert.Contains("group-node: root", standardOutput, StringComparison.Ordinal);
        Assert.Contains("sessions-revoked: 1", standardOutput, StringComparison.Ordinal);
        Assert.Contains("audit: admin_password_recovery_succeeded", standardOutput, StringComparison.Ordinal);
        Assert.NotNull(session.RevokedAtUtc);
        Assert.DoesNotContain(oldPassword, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(newPassword, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(oldHash, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(user.PasswordHash, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(sessionAccessTokenHash, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(sessionRefreshTokenHash, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", standardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", standardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", standardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncIdentityAdminResetPasswordFailsSafelyWhenDisabled()
    {
        using var recoveryEnabledScope = new EnvironmentVariableScope(
            IdentityAdminPasswordRecoveryMaintenanceService.EnabledEnvironmentVariableName,
            null);
        using var loginScope = new EnvironmentVariableScope(
            IdentityAdminPasswordRecoveryMaintenanceService.LoginEnvironmentVariableName,
            "incident-routing-admin");
        using var passwordScope = new EnvironmentVariableScope(
            IdentityAdminPasswordRecoveryMaintenanceService.PasswordEnvironmentVariableName,
            CreateEphemeralSecret());

        var databaseName = $"app-maintenance-program-admin-recovery-disabled-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            await SeedInteractiveRootAdminAsync(
                setupContext,
                new PasswordHasher<AuthUser>(),
                "incident-routing-admin",
                CreateEphemeralSecret());
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await AppMaintenanceProgram.RunAsync(
            ["identity-admin", "reset-password"],
            () => CreateHost(databaseName, databaseRoot),
            stdout,
            stderr,
            CancellationToken.None);

        var standardOutput = stdout.ToString();
        var errorOutput = stderr.ToString();

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, standardOutput);
        Assert.Contains(
            IdentityAdminPasswordRecoveryMaintenanceService.EnabledEnvironmentVariableName,
            errorOutput,
            StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", errorOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncIdentityBootstrapFirstAdminWritesSafeOutputOnly()
    {
        const string login = "first-admin-smoke";
        var password = CreateEphemeralSecret();

        using var bootstrapEnabledScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.EnabledEnvironmentVariableName,
            "true");
        using var loginScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.LoginEnvironmentVariableName,
            login);
        using var passwordScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.PasswordEnvironmentVariableName,
            password);
        using var displayNameScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.DisplayNameEnvironmentVariableName,
            "First Admin Smoke");

        var databaseName = $"app-maintenance-program-first-admin-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            await SeedRoleAndRootAsync(setupContext);
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await AppMaintenanceProgram.RunAsync(
            ["identity-bootstrap", "first-admin"],
            () => CreateHost(databaseName, databaseRoot),
            stdout,
            stderr,
            CancellationToken.None);

        await using var assertContext = CreateDbContext(databaseName, databaseRoot);
        var user = await assertContext.AuthUsers
            .SingleAsync(item => item.NormalizedLogin == "FIRST-ADMIN-SMOKE");

        var standardOutput = stdout.ToString();
        var errorOutput = stderr.ToString();

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, errorOutput);
        Assert.Contains("login: first-admin-smoke", standardOutput, StringComparison.Ordinal);
        Assert.Contains("status: created", standardOutput, StringComparison.Ordinal);
        Assert.Contains("role: platform_owner", standardOutput, StringComparison.Ordinal);
        Assert.Contains("role-link: created", standardOutput, StringComparison.Ordinal);
        Assert.Contains("group-node: root", standardOutput, StringComparison.Ordinal);
        Assert.Contains("assignment: created", standardOutput, StringComparison.Ordinal);
        Assert.Contains("audit: first_admin_bootstrapped", standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(password, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(user.PasswordHash, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", standardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", standardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", standardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncIdentityBootstrapFirstAdminFailsSafelyWhenDisabled()
    {
        using var bootstrapEnabledScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.EnabledEnvironmentVariableName,
            null);
        using var loginScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.LoginEnvironmentVariableName,
            "first-admin-smoke");
        using var passwordScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.PasswordEnvironmentVariableName,
            CreateEphemeralSecret());
        using var displayNameScope = new EnvironmentVariableScope(
            FirstAdminBootstrapMaintenanceService.DisplayNameEnvironmentVariableName,
            null);

        var databaseName = $"app-maintenance-program-first-admin-disabled-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            await SeedRoleAndRootAsync(setupContext);
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await AppMaintenanceProgram.RunAsync(
            ["identity-bootstrap", "first-admin"],
            () => CreateHost(databaseName, databaseRoot),
            stdout,
            stderr,
            CancellationToken.None);

        var standardOutput = stdout.ToString();
        var errorOutput = stderr.ToString();

        await using var assertContext = CreateDbContext(databaseName, databaseRoot);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, standardOutput);
        Assert.Contains(
            FirstAdminBootstrapMaintenanceService.EnabledEnvironmentVariableName,
            errorOutput,
            StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.False(await assertContext.AuthUsers.AnyAsync());
        Assert.False(await assertContext.AuditRecords.AnyAsync());
    }

    [Fact]
    public async Task RunAsyncIncidentRoutingAdminUpsertWritesSafeOutputOnly()
    {
        const string password = "S2-31-safe-output-password!";

        using var passwordScope = new EnvironmentVariableScope(
            IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName,
            password);

        var databaseName = $"app-maintenance-program-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            await SeedRoleAndRootAsync(setupContext);
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await AppMaintenanceProgram.RunAsync(
            ["incident-routing-admin", "upsert"],
            () => CreateHost(databaseName, databaseRoot),
            stdout,
            stderr,
            CancellationToken.None);

        await using var assertContext = CreateDbContext(databaseName, databaseRoot);
        var user = await assertContext.AuthUsers
            .SingleAsync(item => item.NormalizedLogin == "INCIDENT-ROUTING-ADMIN");

        var standardOutput = stdout.ToString();
        var errorOutput = stderr.ToString();

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, errorOutput);
        Assert.Contains("login: incident-routing-admin", standardOutput, StringComparison.Ordinal);
        Assert.Contains("status: created", standardOutput, StringComparison.Ordinal);
        Assert.Contains("role: platform_owner", standardOutput, StringComparison.Ordinal);
        Assert.Contains("role-link: created", standardOutput, StringComparison.Ordinal);
        Assert.Contains("group-node: root", standardOutput, StringComparison.Ordinal);
        Assert.Contains("assignment: created", standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(password, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(user.PasswordHash, standardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", standardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", standardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncIncidentRoutingAdminUpsertFailsSafelyWhenPasswordIsMissing()
    {
        using var passwordScope = new EnvironmentVariableScope(
            IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName,
            null);

        var databaseName = $"app-maintenance-program-missing-password-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            await SeedRoleAndRootAsync(setupContext);
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await AppMaintenanceProgram.RunAsync(
            ["incident-routing-admin", "upsert"],
            () => CreateHost(databaseName, databaseRoot),
            stdout,
            stderr,
            CancellationToken.None);

        var standardOutput = stdout.ToString();
        var errorOutput = stderr.ToString();

        await using var assertContext = CreateDbContext(databaseName, databaseRoot);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, standardOutput);
        Assert.Contains(
            IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName,
            errorOutput,
            StringComparison.Ordinal);
        Assert.DoesNotContain("accessToken", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", errorOutput, StringComparison.OrdinalIgnoreCase);
        Assert.False(await assertContext.AuthUsers.AnyAsync());
        Assert.False(await assertContext.GroupAdminAssignments.AnyAsync());
    }

    private static IHost CreateHost(
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddDbContext<PlatformDbContext>(
            options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        builder.Services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
        builder.Services.AddScoped<IncidentRoutingAdminMaintenanceService>();
        builder.Services.AddScoped<FirstAdminBootstrapMaintenanceService>();
        builder.Services.AddScoped<IdentityAdminPasswordRecoveryMaintenanceService>();
        builder.Services.AddScoped<IntegrationAccountMaintenanceService>();

        return builder.Build();
    }

    private static async Task<AuthUser> SeedInteractiveRootAdminAsync(
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

        return user;
    }

    private static async Task AddAuthSessionAsync(
        PlatformDbContext dbContext,
        Guid userId,
        string accessTokenHash,
        string refreshTokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset refreshExpiresAtUtc)
    {
        dbContext.AuthSessions.Add(new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = Guid.NewGuid(),
            AccessTokenHash = accessTokenHash,
            RefreshTokenHash = refreshTokenHash,
            IsOfflineRestricted = false,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = expiresAtUtc,
            RefreshExpiresAtUtc = refreshExpiresAtUtc
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRoleAndRootAsync(PlatformDbContext dbContext)
    {
        dbContext.AuthRoles.Add(new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = IncidentRoutingAdminMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        });
        dbContext.GroupNodes.Add(new GroupNode
        {
            Id = Guid.NewGuid(),
            Code = IncidentRoutingAdminMaintenanceService.RootGroupNodeCode,
            Name = "Root",
            Depth = 0,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
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