using System.ComponentModel.DataAnnotations;

using BuildingBlocks.Contracts.Devices;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Devices;
using BuildingBlocks.Infrastructure.PlatformRuntime;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.Devices;

public sealed class DevicesOptions
{
    public bool AllowTrustedRegistration { get; init; } = true;

    [Range(1, 3650)]
    public int DeviceSeenRetentionDays { get; init; } = 365;
}

public sealed record DeviceRegistrationUpsertedInternalEvent(
    Guid DeviceId,
    Guid? LastKnownUserId,
    bool IsTrusted,
    DateTimeOffset OccurredAtUtc) : IInternalEvent;

public interface IDeviceRegistrationService
{
    Task<DeviceRegistrationInfoDto> UpsertAsync(
        DeviceRegistrationUpsertRequestDto request,
        CancellationToken cancellationToken);
}

public static class DevicesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddDevicesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatedModuleOptions<DevicesOptions>(
            configuration,
            "Modules:Devices");

        services.AddScoped<IDeviceRegistrationService, DeviceRegistrationService>();
        services.AddScoped<IInternalEventHandler<DeviceRegistrationUpsertedInternalEvent>, DeviceRegistrationUpsertedLoggingHandler>();

        return services;
    }
}

internal sealed class DeviceRegistrationService(
    PlatformDbContext dbContext,
    IOptions<DevicesOptions> options,
    IFeatureFlagService featureFlags,
    IInternalEventPublisher internalEventPublisher) : IDeviceRegistrationService
{
    public async Task<DeviceRegistrationInfoDto> UpsertAsync(
        DeviceRegistrationUpsertRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!featureFlags.IsEnabled(PlatformFeatureFlags.DevicesDeviceRegistrationEnabled))
        {
            throw new FeatureDisabledException(PlatformFeatureFlags.DevicesDeviceRegistrationEnabled);
        }

        var now = DateTimeOffset.UtcNow;

        var entity = await dbContext.DeviceRegistrations
            .SingleOrDefaultAsync(
                item => item.DeviceId == request.DeviceId,
                cancellationToken);

        if (entity is null)
        {
            entity = new DeviceRegistration
            {
                DeviceId = request.DeviceId,
                RegisteredAtUtc = now
            };

            dbContext.DeviceRegistrations.Add(entity);
        }

        entity.Platform = request.Platform;
        entity.DeviceName = request.DeviceName;
        entity.Model = request.Model;
        entity.OsVersion = request.OsVersion;
        entity.LastKnownUserId = request.LastKnownUserId;
        entity.IsTrusted = options.Value.AllowTrustedRegistration && request.IsTrusted;
        entity.LastSeenAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await internalEventPublisher.PublishAsync(
            new DeviceRegistrationUpsertedInternalEvent(
                entity.DeviceId,
                entity.LastKnownUserId,
                entity.IsTrusted,
                now),
            cancellationToken);

        return new DeviceRegistrationInfoDto(
            entity.DeviceId,
            entity.Platform,
            entity.DeviceName,
            entity.Model,
            entity.OsVersion,
            entity.IsTrusted,
            entity.LastKnownUserId,
            entity.RegisteredAtUtc,
            entity.LastSeenAtUtc);
    }
}

internal sealed class DeviceRegistrationUpsertedLoggingHandler(
    ILogger<DeviceRegistrationUpsertedLoggingHandler> logger)
    : IInternalEventHandler<DeviceRegistrationUpsertedInternalEvent>
{
    public Task HandleAsync(
        DeviceRegistrationUpsertedInternalEvent internalEvent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Handled internal event DeviceRegistrationUpsertedInternalEvent. DeviceId={DeviceId} LastKnownUserId={LastKnownUserId} IsTrusted={IsTrusted}",
            internalEvent.DeviceId,
            internalEvent.LastKnownUserId,
            internalEvent.IsTrusted);

        return Task.CompletedTask;
    }
}