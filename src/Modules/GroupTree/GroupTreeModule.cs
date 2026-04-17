using BuildingBlocks.Contracts.Groups;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.GroupTree;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.GroupTree;

public sealed class GroupTreeOptions
{
    public string DevelopmentBootstrapAdminLogin { get; init; } = "platform-owner";
}

public interface IGroupTreeQueryService
{
    Task<IReadOnlyCollection<GroupNodeFlatDto>> GetNodesAsync(CancellationToken cancellationToken);

    Task<GroupRoutingResultDto?> PreviewRoutingAsync(
        Guid groupNodeId,
        Guid uploaderUserId,
        CancellationToken cancellationToken);
}

public static class GroupTreeModuleServiceCollectionExtensions
{
    public static IServiceCollection AddGroupTreeModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<GroupTreeOptions>()
            .Bind(configuration.GetSection("Modules:GroupTree"));

        services.AddScoped<IGroupTreeQueryService, GroupTreeQueryService>();

        if (environment.IsDevelopment())
        {
            services.AddHostedService<DevelopmentGroupTreeBootstrapService>();
        }

        return services;
    }
}

internal sealed class DevelopmentGroupTreeBootstrapService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IOptions<GroupTreeOptions> options,
    ILogger<DevelopmentGroupTreeBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

            var adminUser = await dbContext.AuthUsers
                .SingleOrDefaultAsync(
                    item => item.NormalizedLogin == options.Value.DevelopmentBootstrapAdminLogin.Trim().ToUpperInvariant(),
                    cancellationToken);

            if (adminUser is not null)
            {
                var targetNodeCodes = new[] { "root", "branch-a" };

                var nodes = await dbContext.GroupNodes
                    .Where(item => targetNodeCodes.Contains(item.Code))
                    .ToArrayAsync(cancellationToken);

                foreach (var node in nodes)
                {
                    var exists = await dbContext.GroupAdminAssignments.AnyAsync(
                        item => item.GroupNodeId == node.Id && item.UserId == adminUser.Id,
                        cancellationToken);

                    if (!exists)
                    {
                        dbContext.GroupAdminAssignments.Add(new GroupAdminAssignment
                        {
                            GroupNodeId = node.Id,
                            UserId = adminUser.Id,
                            AssignedAtUtc = DateTimeOffset.UtcNow
                        });
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Group tree development bootstrap ensured assignments for Login={Login}.",
                    adminUser.Login);

                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        logger.LogWarning(
            "Group tree development bootstrap skipped because admin login {Login} was not found.",
            options.Value.DevelopmentBootstrapAdminLogin);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

internal sealed class GroupTreeQueryService(PlatformDbContext dbContext) : IGroupTreeQueryService
{
    public async Task<IReadOnlyCollection<GroupNodeFlatDto>> GetNodesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.GroupNodes
            .OrderBy(item => item.Depth)
            .ThenBy(item => item.Code)
            .Select(item => new GroupNodeFlatDto(
                item.Id,
                item.ParentNodeId,
                item.Code,
                item.Name,
                item.Depth,
                item.IsActive))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<GroupRoutingResultDto?> PreviewRoutingAsync(
        Guid groupNodeId,
        Guid uploaderUserId,
        CancellationToken cancellationToken)
    {
        var nodes = await dbContext.GroupNodes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var assignments = await dbContext.GroupAdminAssignments
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var nodesById = nodes.ToDictionary(item => item.Id);

        if (!nodesById.TryGetValue(groupNodeId, out var currentNode))
        {
            return null;
        }

        var currentNodeId = currentNode.Id;
        var escalatedHigher = false;

        while (true)
        {
            var adminUserIds = assignments
                .Where(item => item.GroupNodeId == currentNodeId)
                .Select(item => item.UserId)
                .Distinct()
                .ToArray();

            var uploaderIsAdminHere = adminUserIds.Contains(uploaderUserId);

            if ((adminUserIds.Length > 0 && !uploaderIsAdminHere) || currentNode.ParentNodeId is null)
            {
                return new GroupRoutingResultDto(
                    groupNodeId,
                    currentNodeId,
                    adminUserIds,
                    escalatedHigher);
            }

            escalatedHigher = true;

            if (!nodesById.TryGetValue(currentNode.ParentNodeId.Value, out currentNode))
            {
                return new GroupRoutingResultDto(
                    groupNodeId,
                    currentNodeId,
                    adminUserIds,
                    escalatedHigher);
            }

            currentNodeId = currentNode.Id;
        }
    }
}