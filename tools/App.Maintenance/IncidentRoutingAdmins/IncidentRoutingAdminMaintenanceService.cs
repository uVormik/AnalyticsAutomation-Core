using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Maintenance.IncidentRoutingAdmins;

public sealed class IncidentRoutingAdminMaintenanceService(
    PlatformDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher)
{
    public const string PasswordEnvironmentVariableName = "AA_INCIDENT_ROUTING_ADMIN_PASSWORD";
    public const string IncidentRoutingAdminLogin = "incident-routing-admin";
    public const string IncidentRoutingAdminDisplayName = "Incident Routing Admin";
    public const string PlatformOwnerRoleCode = "platform_owner";
    public const string RootGroupNodeCode = "root";

    public async Task<IncidentRoutingAdminUpsertResult> UpsertAsync(CancellationToken cancellationToken)
    {
        var password = ReadRequiredPassword();
        var role = await dbContext.AuthRoles
            .SingleOrDefaultAsync(
                item => item.Code == PlatformOwnerRoleCode,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Role '{PlatformOwnerRoleCode}' was not found. Aborting without changes.");

        var rootGroupNode = await dbContext.GroupNodes
            .SingleOrDefaultAsync(
                item => item.Code == RootGroupNodeCode,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Group node '{RootGroupNodeCode}' was not found. Aborting without changes.");

        var normalizedLogin = NormalizeLogin(IncidentRoutingAdminLogin);
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

        user.Login = IncidentRoutingAdminLogin;
        user.NormalizedLogin = normalizedLogin;
        user.DisplayName = IncidentRoutingAdminDisplayName;
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return new IncidentRoutingAdminUpsertResult(
            user.Login,
            wasCreated,
            role.Code,
            wasRoleLinkCreated,
            rootGroupNode.Code,
            wasAssignmentCreated);
    }

    private static string ReadRequiredPassword()
    {
        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Environment variable {PasswordEnvironmentVariableName} is required. The tool does not prompt for a password.");
        }

        return password;
    }

    private static string NormalizeLogin(string login)
    {
        return login.Trim().ToUpperInvariant();
    }
}

public sealed record IncidentRoutingAdminUpsertResult(
    string Login,
    bool WasCreated,
    string AssignedRoleCode,
    bool WasRoleLinkCreated,
    string AssignedGroupNodeCode,
    bool WasAssignmentCreated);