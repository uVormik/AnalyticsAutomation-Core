using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuildingBlocks.Infrastructure.Persistence.DesignTime;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionStringOverride = Environment.GetEnvironmentVariable("ConnectionStrings__MainDatabase");

        var databaseOptions = new DatabaseOptions
        {
            ConnectionString = connectionStringOverride
        };

        var builder = new DbContextOptionsBuilder<PlatformDbContext>();
        builder.UseNpgsql(
            DatabaseConnectionStringFactory.Build(databaseOptions),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"));

        return new PlatformDbContext(builder.Options);
    }
}