using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Modules.Auth.Configuration;

namespace Modules.Auth.Services;

public sealed class DevelopmentAuthBootstrapService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IOptions<AuthOptions> authOptions,
    ILogger<DevelopmentAuthBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AuthUser>>();

        if (await dbContext.AuthUsers.AnyAsync(cancellationToken))
        {
            logger.LogInformation(
                "Development auth bootstrap skipped because users already exist.");
            return;
        }

        var role = await dbContext.AuthRoles
            .SingleOrDefaultAsync(
                item => item.Code == authOptions.Value.DevelopmentBootstrapRoleCode,
                cancellationToken);

        if (role is null)
        {
            logger.LogWarning(
                "Development auth bootstrap skipped because role code {RoleCode} was not found.",
                authOptions.Value.DevelopmentBootstrapRoleCode);
            return;
        }

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Login = authOptions.Value.DevelopmentBootstrapUserLogin,
            NormalizedLogin = authOptions.Value.DevelopmentBootstrapUserLogin.Trim().ToUpperInvariant(),
            DisplayName = authOptions.Value.DevelopmentBootstrapUserDisplayName,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            authOptions.Value.DevelopmentBootstrapUserPassword);

        dbContext.AuthUsers.Add(user);
        dbContext.AuthUserRoles.Add(new AuthUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Development auth bootstrap user created. Login={Login}",
            user.Login);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}