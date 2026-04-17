using BuildingBlocks.Contracts.Devices;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Entities.Devices;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Modules.Devices;

public sealed class DevicesOptions
{
    public bool AllowTrustedRegistration { get; init; } = true;
}

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
        services.AddOptions<DevicesOptions>()
            .Bind(configuration.GetSection("Modules:Devices"));

        services.AddScoped<IDeviceRegistrationService, DeviceRegistrationService>();

        return services;
    }
}

internal sealed class DeviceRegistrationService(
    PlatformDbContext dbContext,
    IOptions<DevicesOptions> options) : IDeviceRegistrationService
{
    public async Task<DeviceRegistrationInfoDto> UpsertAsync(
        DeviceRegistrationUpsertRequestDto request,
        CancellationToken cancellationToken)
    {
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