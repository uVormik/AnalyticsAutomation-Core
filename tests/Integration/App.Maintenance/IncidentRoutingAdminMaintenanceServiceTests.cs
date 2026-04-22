using App.Maintenance.IncidentRoutingAdmins;

using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Xunit;

namespace App.Maintenance.Tests;

public sealed class IncidentRoutingAdminMaintenanceServiceTests
{
    [Fact]
    public async Task UpsertAsyncCreatesIncidentRoutingAdminWithRootGroupRoleAndAssignment()
    {
        const string password = "S2-31-create-password!";
        const string normalizedLogin = "INCIDENT-ROUTING-ADMIN";

        using var passwordScope = new EnvironmentVariableScope(
            IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName,
            password);

        using var dbContext = CreateDbContext();
        var role = new AuthRole
        {
            Id = Guid.NewGuid(),
            Code = IncidentRoutingAdminMaintenanceService.PlatformOwnerRoleCode,
            Name = "Platform Owner"
        };
        var rootNode = new GroupNode
        {
            Id = Guid.NewGuid(),
            Code = IncidentRoutingAdminMaintenanceService.RootGroupNodeCode,
            Name = "Root",
            Depth = 0,
            IsActive = true
        };

        dbContext.AuthRoles.Add(role);
        dbContext.GroupNodes.Add(rootNode);
        await dbContext.SaveChangesAsync();

        var passwordHasher = new PasswordHasher<AuthUser>();
        var service = new IncidentRoutingAdminMaintenanceService(dbContext, passwordHasher);

        var result = await service.UpsertAsync(CancellationToken.None);

        dbContext.ChangeTracker.Clear();

        var user = await dbContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleAsync(item => item.NormalizedLogin == normalizedLogin);
        var assignment = await dbContext.GroupAdminAssignments
            .SingleAsync(item => item.GroupNodeId == rootNode.Id && item.UserId == user.Id);

        Assert.True(result.WasCreated);
        Assert.True(result.WasRoleLinkCreated);
        Assert.True(result.WasAssignmentCreated);
        Assert.Equal(IncidentRoutingAdminMaintenanceService.IncidentRoutingAdminLogin, result.Login);
        Assert.Equal(IncidentRoutingAdminMaintenanceService.PlatformOwnerRoleCode, result.AssignedRoleCode);
        Assert.Equal(IncidentRoutingAdminMaintenanceService.RootGroupNodeCode, result.AssignedGroupNodeCode);
        Assert.Equal(IncidentRoutingAdminMaintenanceService.IncidentRoutingAdminDisplayName, user.DisplayName);
        Assert.True(user.IsActive);
        Assert.Equal(rootNode.Id, user.CurrentGroupNodeId);
        Assert.Single(user.UserRoles);
        Assert.Equal(role.Id, user.UserRoles.Single().RoleId);
        Assert.Equal(rootNode.Id, assignment.GroupNodeId);
        Assert.Equal(user.Id, assignment.UserId);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password));
    }

    [Fact]
    public async Task UpsertAsyncIsIdempotentOnSecondRun()
    {
        const string password = "S2-31-idempotent-password!";
        const string databaseName = "incident-routing-admin-idempotent";

        using var passwordScope = new EnvironmentVariableScope(
            IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName,
            password);

        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var setupContext = CreateDbContext(databaseName, databaseRoot))
        {
            await SeedRoleAndRootAsync(setupContext);
        }

        IncidentRoutingAdminUpsertResult firstResult;
        await using (var firstContext = CreateDbContext(databaseName, databaseRoot))
        {
            var service = new IncidentRoutingAdminMaintenanceService(
                firstContext,
                new PasswordHasher<AuthUser>());

            firstResult = await service.UpsertAsync(CancellationToken.None);
        }

        IncidentRoutingAdminUpsertResult secondResult;
        await using (var secondContext = CreateDbContext(databaseName, databaseRoot))
        {
            var service = new IncidentRoutingAdminMaintenanceService(
                secondContext,
                new PasswordHasher<AuthUser>());

            secondResult = await service.UpsertAsync(CancellationToken.None);
        }

        await using var assertContext = CreateDbContext(databaseName, databaseRoot);
        var user = await assertContext.AuthUsers
            .Include(item => item.UserRoles)
            .SingleAsync(item => item.NormalizedLogin == "INCIDENT-ROUTING-ADMIN");

        Assert.True(firstResult.WasCreated);
        Assert.True(firstResult.WasRoleLinkCreated);
        Assert.True(firstResult.WasAssignmentCreated);
        Assert.False(secondResult.WasCreated);
        Assert.False(secondResult.WasRoleLinkCreated);
        Assert.False(secondResult.WasAssignmentCreated);
        Assert.True(user.IsActive);
        Assert.Equal(1, await assertContext.AuthUsers.CountAsync());
        Assert.Equal(1, await assertContext.AuthUserRoles.CountAsync());
        Assert.Equal(1, await assertContext.GroupAdminAssignments.CountAsync());
    }

    [Fact]
    public async Task UpsertAsyncThrowsWhenPasswordEnvironmentVariableIsMissing()
    {
        using var passwordScope = new EnvironmentVariableScope(
            IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName,
            null);

        using var dbContext = CreateDbContext();
        await SeedRoleAndRootAsync(dbContext);

        var service = new IncidentRoutingAdminMaintenanceService(
            dbContext,
            new PasswordHasher<AuthUser>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpsertAsync(CancellationToken.None));

        Assert.Equal(
            $"Environment variable {IncidentRoutingAdminMaintenanceService.PasswordEnvironmentVariableName} is required. The tool does not prompt for a password.",
            exception.Message);
        Assert.False(await dbContext.AuthUsers.AnyAsync());
        Assert.False(await dbContext.GroupAdminAssignments.AnyAsync());
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

    private static PlatformDbContext CreateDbContext()
    {
        return CreateDbContext($"incident-routing-admin-tests-{Guid.NewGuid():N}", new InMemoryDatabaseRoot());
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