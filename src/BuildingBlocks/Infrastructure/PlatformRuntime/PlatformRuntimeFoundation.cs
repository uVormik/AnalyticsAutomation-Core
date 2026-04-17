using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.PlatformRuntime;

public static class PlatformFeatureFlags
{
    public const string AuthDevelopmentBootstrapEnabled = "Modules.Auth.DevelopmentBootstrapEnabled";
    public const string DevicesDeviceRegistrationEnabled = "Modules.Devices.DeviceRegistrationEnabled";

    public static readonly IReadOnlyCollection<string> All =
    [
        AuthDevelopmentBootstrapEnabled,
        DevicesDeviceRegistrationEnabled
    ];
}

public interface IFeatureFlagService
{
    bool IsEnabled(string flagName);
}

public sealed class FeatureDisabledException(string flagName)
    : InvalidOperationException($"Feature flag '{flagName}' is disabled.")
{
    public string FlagName { get; } = flagName;
}

public interface IInternalEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}

public interface IInternalEventHandler<in TEvent>
    where TEvent : class, IInternalEvent
{
    Task HandleAsync(TEvent internalEvent, CancellationToken cancellationToken);
}

public interface IInternalEventPublisher
{
    Task PublishAsync<TEvent>(TEvent internalEvent, CancellationToken cancellationToken)
        where TEvent : class, IInternalEvent;
}

public static class PlatformRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformRuntimeFoundation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IFeatureFlagService, ConfigurationFeatureFlagService>();
        services.AddHostedService<FeatureFlagStartupValidationService>();
        services.AddScoped<IInternalEventPublisher, InProcessInternalEventPublisher>();

        return services;
    }

    public static OptionsBuilder<TOptions> AddValidatedModuleOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath)
        where TOptions : class, new()
    {
        return services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .Validate(
                options => TryValidate(options),
                $"Invalid configuration for section '{sectionPath}'.")
            .ValidateOnStart();
    }

    private static bool TryValidate<TOptions>(TOptions options)
    {
        var context = new ValidationContext(options!);
        var results = new List<ValidationResult>();

        return Validator.TryValidateObject(
            options!,
            context,
            results,
            validateAllProperties: true);
    }
}

internal sealed class ConfigurationFeatureFlagService(IConfiguration configuration) : IFeatureFlagService
{
    public bool IsEnabled(string flagName)
    {
        var rawValue = configuration[$"FeatureFlags:{flagName.Replace('.', ':')}"];

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        return bool.TryParse(rawValue, out var enabled) && enabled;
    }
}

internal sealed class FeatureFlagStartupValidationService(
    IConfiguration configuration,
    ILogger<FeatureFlagStartupValidationService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var flagName in PlatformFeatureFlags.All)
        {
            var rawValue = configuration[$"FeatureFlags:{flagName.Replace('.', ':')}"];

            if (!string.IsNullOrWhiteSpace(rawValue) && !bool.TryParse(rawValue, out _))
            {
                throw new OptionsValidationException(
                    nameof(PlatformFeatureFlags),
                    typeof(PlatformFeatureFlags),
                    [$"Feature flag '{flagName}' must be boolean."]);
            }

            logger.LogInformation(
                "Feature flag {FeatureFlag} startup state is {FeatureFlagState}.",
                flagName,
                string.IsNullOrWhiteSpace(rawValue) ? "unset" : rawValue);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

internal sealed class InProcessInternalEventPublisher(
    IServiceProvider serviceProvider,
    ILogger<InProcessInternalEventPublisher> logger) : IInternalEventPublisher
{
    public async Task PublishAsync<TEvent>(
        TEvent internalEvent,
        CancellationToken cancellationToken)
        where TEvent : class, IInternalEvent
    {
        var handlers = serviceProvider.GetServices<IInternalEventHandler<TEvent>>().ToArray();

        logger.LogInformation(
            "Publishing internal event {EventType} to {HandlersCount} handlers.",
            typeof(TEvent).Name,
            handlers.Length);

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(internalEvent, cancellationToken);
        }
    }
}