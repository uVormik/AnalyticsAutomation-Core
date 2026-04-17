using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;
using BuildingBlocks.Infrastructure.Persistence.Entities.Devices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceRegistrationConfiguration : IEntityTypeConfiguration<DeviceRegistration>
{
    public void Configure(EntityTypeBuilder<DeviceRegistration> builder)
    {
        builder.ToTable("device_registrations");

        builder.HasKey(item => item.DeviceId)
            .HasName("pk_device_registrations");

        builder.Property(item => item.DeviceId).HasColumnName("device_id");
        builder.Property(item => item.Platform).HasColumnName("platform").HasMaxLength(64).IsRequired();
        builder.Property(item => item.DeviceName).HasColumnName("device_name").HasMaxLength(256).IsRequired();
        builder.Property(item => item.Model).HasColumnName("model").HasMaxLength(256);
        builder.Property(item => item.OsVersion).HasColumnName("os_version").HasMaxLength(128);
        builder.Property(item => item.IsTrusted).HasColumnName("is_trusted").IsRequired();
        builder.Property(item => item.LastKnownUserId).HasColumnName("last_known_user_id");
        builder.Property(item => item.RegisteredAtUtc).HasColumnName("registered_at_utc").IsRequired();
        builder.Property(item => item.LastSeenAtUtc).HasColumnName("last_seen_at_utc").IsRequired();

        builder.HasOne<AuthUser>()
            .WithMany()
            .HasForeignKey(item => item.LastKnownUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_device_registrations_auth_users_last_known_user_id");
    }
}