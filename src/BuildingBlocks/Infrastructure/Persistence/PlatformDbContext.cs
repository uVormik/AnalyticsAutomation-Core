using BuildingBlocks.Infrastructure.Persistence.Entities;
using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<DatabaseBootstrapMarker> DatabaseBootstrapMarkers => Set<DatabaseBootstrapMarker>();

    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<AuthRole> AuthRoles => Set<AuthRole>();
    public DbSet<AuthPermission> AuthPermissions => Set<AuthPermission>();
    public DbSet<AuthUserRole> AuthUserRoles => Set<AuthUserRole>();
    public DbSet<AuthRolePermission> AuthRolePermissions => Set<AuthRolePermission>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<AuthLastActiveDeviceAccount> AuthLastActiveDeviceAccounts => Set<AuthLastActiveDeviceAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
    }
}