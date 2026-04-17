using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Modules.Auth.Configuration;
using Modules.Auth.Services;

namespace Modules.Auth;

public static class AuthModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection("Modules:Auth"))
            .Validate(
                options => options.AccessTokenLifetimeMinutes > 0
                    && options.RefreshTokenLifetimeHours > 0
                    && !string.IsNullOrWhiteSpace(options.DevelopmentBootstrapUserLogin)
                    && !string.IsNullOrWhiteSpace(options.DevelopmentBootstrapUserPassword)
                    && !string.IsNullOrWhiteSpace(options.DevelopmentBootstrapRoleCode),
                "Modules:Auth configuration is invalid.");

        services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
        services.AddScoped<IAuthService, AuthService>();

        if (environment.IsDevelopment())
        {
            services.AddHostedService<DevelopmentAuthBootstrapService>();
        }

        return services;
    }
}