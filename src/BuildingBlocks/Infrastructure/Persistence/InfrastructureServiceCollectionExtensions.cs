using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformPersistence(
        this IServiceCollection services,
        DatabaseOptions databaseOptions)
    {
        services.AddSingleton(databaseOptions);

        services.AddDbContext<PlatformDbContext>((serviceProvider, dbContextOptions) =>
        {
            var resolvedOptions = serviceProvider.GetRequiredService<DatabaseOptions>();

            dbContextOptions.UseNpgsql(
                DatabaseConnectionStringFactory.Build(resolvedOptions),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"));
        });

        return services;
    }
}