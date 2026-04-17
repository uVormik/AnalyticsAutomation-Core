using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.PlatformRuntime;

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
        services.AddValidatedModuleOptions<AuthOptions>(
            configuration,
            "Modules:Auth");

        services.AddSingleton<IPasswordHasher<AuthUser>, PasswordHasher<AuthUser>>();
        services.AddScoped<IAuthService, AuthService>();

        if (environment.IsDevelopment())
        {
            services.AddHostedService<DevelopmentAuthBootstrapService>();
        }

        return services;
    }
}