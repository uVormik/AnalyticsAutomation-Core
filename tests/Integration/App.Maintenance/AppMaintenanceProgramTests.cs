using App.Maintenance.IncidentRoutingAdmins;
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
        builder.Services.AddScoped<IntegrationAccountMaintenanceService>();

        return builder.Build();
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