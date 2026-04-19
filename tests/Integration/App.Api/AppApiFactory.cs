using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace App.Api.IntegrationTests;

public sealed class AppApiFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        string databaseName = $"AppApiIntegrationTests-{Guid.NewGuid():N}";

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(static (_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureFlags:Modules.Auth.DevelopmentBootstrapEnabled"] = "false",
                ["Modules:Auth:DevelopmentBootstrapEnabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            foreach (ServiceDescriptor serviceDescriptor in services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType?.Name == "DevelopmentAuthBootstrapService")
                .ToArray())
            {
                services.Remove(serviceDescriptor);
            }

            services.RemoveAll<DbContextOptions<PlatformDbContext>>();
            services.RemoveAll<PlatformDbContext>();

            services.AddDbContext<PlatformDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}