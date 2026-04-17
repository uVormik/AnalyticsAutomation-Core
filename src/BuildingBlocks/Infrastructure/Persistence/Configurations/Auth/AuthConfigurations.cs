using BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Infrastructure.Persistence.Configurations.Auth;

internal static class AuthSeedData
{
    public static readonly Guid PlatformOwnerRoleId = Guid.Parse("7A9170BC-6A0A-4A03-A948-0F6AF0E38E78");
    public static readonly Guid SignInPermissionId = Guid.Parse("402957AF-25C0-455B-A753-1576A5ECFA91");
    public static readonly Guid RefreshPermissionId = Guid.Parse("6A6F1E27-B895-423A-AF08-F0A1D8E77B0D");
    public static readonly Guid LastActiveReadPermissionId = Guid.Parse("34D52B4E-D652-4D27-9F4A-6D6F706CE3C0");
}

public sealed class AuthUserConfiguration : IEntityTypeConfiguration<AuthUser>
{
    public void Configure(EntityTypeBuilder<AuthUser> builder)
    {
        builder.ToTable("auth_users");

        builder.HasKey(item => item.Id)
            .HasName("pk_auth_users");

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.Login)
            .HasColumnName("login")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.NormalizedLogin)
            .HasColumnName("normalized_login")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(item => item.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(item => item.CurrentGroupNodeId)
            .HasColumnName("current_group_node_id");

        builder.Property(item => item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(item => item.LastSignInAtUtc)
            .HasColumnName("last_sign_in_at_utc");

        builder.HasIndex(item => item.NormalizedLogin)
            .HasDatabaseName("ux_auth_users_normalized_login")
            .IsUnique();
    }
}

public sealed class AuthRoleConfiguration : IEntityTypeConfiguration<AuthRole>
{
    public void Configure(EntityTypeBuilder<AuthRole> builder)
    {
        builder.ToTable("auth_roles");

        builder.HasKey(item => item.Id)
            .HasName("pk_auth_roles");

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(item => item.Code)
            .HasDatabaseName("ux_auth_roles_code")
            .IsUnique();

        builder.HasData(new AuthRole
        {
            Id = AuthSeedData.PlatformOwnerRoleId,
            Code = "platform_owner",
            Name = "Platform Owner"
        });
    }
}

public sealed class AuthPermissionConfiguration : IEntityTypeConfiguration<AuthPermission>
{
    public void Configure(EntityTypeBuilder<AuthPermission> builder)
    {
        builder.ToTable("auth_permissions");

        builder.HasKey(item => item.Id)
            .HasName("pk_auth_permissions");

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(item => item.Code)
            .HasDatabaseName("ux_auth_permissions_code")
            .IsUnique();

        builder.HasData(
            new AuthPermission
            {
                Id = AuthSeedData.SignInPermissionId,
                Code = "auth.session.sign_in",
                Name = "Auth Session Sign In"
            },
            new AuthPermission
            {
                Id = AuthSeedData.RefreshPermissionId,
                Code = "auth.session.refresh",
                Name = "Auth Session Refresh"
            },
            new AuthPermission
            {
                Id = AuthSeedData.LastActiveReadPermissionId,
                Code = "auth.last_active_device_account.read",
                Name = "Auth Last Active Device Account Read"
            });
    }
}

public sealed class AuthUserRoleConfiguration : IEntityTypeConfiguration<AuthUserRole>
{
    public void Configure(EntityTypeBuilder<AuthUserRole> builder)
    {
        builder.ToTable("auth_user_roles");

        builder.HasKey(item => new { item.UserId, item.RoleId })
            .HasName("pk_auth_user_roles");

        builder.Property(item => item.UserId)
            .HasColumnName("user_id");

        builder.Property(item => item.RoleId)
            .HasColumnName("role_id");

        builder.HasOne(item => item.User)
            .WithMany(item => item.UserRoles)
            .HasForeignKey(item => item.UserId)
            .HasConstraintName("fk_auth_user_roles_users_user_id");

        builder.HasOne(item => item.Role)
            .WithMany(item => item.UserRoles)
            .HasForeignKey(item => item.RoleId)
            .HasConstraintName("fk_auth_user_roles_roles_role_id");
    }
}

public sealed class AuthRolePermissionConfiguration : IEntityTypeConfiguration<AuthRolePermission>
{
    public void Configure(EntityTypeBuilder<AuthRolePermission> builder)
    {
        builder.ToTable("auth_role_permissions");

        builder.HasKey(item => new { item.RoleId, item.PermissionId })
            .HasName("pk_auth_role_permissions");

        builder.Property(item => item.RoleId)
            .HasColumnName("role_id");

        builder.Property(item => item.PermissionId)
            .HasColumnName("permission_id");

        builder.HasOne(item => item.Role)
            .WithMany(item => item.RolePermissions)
            .HasForeignKey(item => item.RoleId)
            .HasConstraintName("fk_auth_role_permissions_roles_role_id");

        builder.HasOne(item => item.Permission)
            .WithMany(item => item.RolePermissions)
            .HasForeignKey(item => item.PermissionId)
            .HasConstraintName("fk_auth_role_permissions_permissions_permission_id");

        builder.HasData(
            new AuthRolePermission
            {
                RoleId = AuthSeedData.PlatformOwnerRoleId,
                PermissionId = AuthSeedData.SignInPermissionId
            },
            new AuthRolePermission
            {
                RoleId = AuthSeedData.PlatformOwnerRoleId,
                PermissionId = AuthSeedData.RefreshPermissionId
            },
            new AuthRolePermission
            {
                RoleId = AuthSeedData.PlatformOwnerRoleId,
                PermissionId = AuthSeedData.LastActiveReadPermissionId
            });
    }
}

public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_sessions");

        builder.HasKey(item => item.Id)
            .HasName("pk_auth_sessions");

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.UserId)
            .HasColumnName("user_id");

        builder.Property(item => item.DeviceId)
            .HasColumnName("device_id");

        builder.Property(item => item.AccessTokenHash)
            .HasColumnName("access_token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.IsOfflineRestricted)
            .HasColumnName("is_offline_restricted")
            .IsRequired();

        builder.Property(item => item.IssuedAtUtc)
            .HasColumnName("issued_at_utc")
            .IsRequired();

        builder.Property(item => item.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(item => item.RefreshExpiresAtUtc)
            .HasColumnName("refresh_expires_at_utc")
            .IsRequired();

        builder.Property(item => item.RevokedAtUtc)
            .HasColumnName("revoked_at_utc");

        builder.HasIndex(item => item.AccessTokenHash)
            .HasDatabaseName("ux_auth_sessions_access_token_hash")
            .IsUnique();

        builder.HasIndex(item => item.RefreshTokenHash)
            .HasDatabaseName("ux_auth_sessions_refresh_token_hash")
            .IsUnique();

        builder.HasOne(item => item.User)
            .WithMany(item => item.Sessions)
            .HasForeignKey(item => item.UserId)
            .HasConstraintName("fk_auth_sessions_users_user_id");
    }
}

public sealed class AuthLastActiveDeviceAccountConfiguration : IEntityTypeConfiguration<AuthLastActiveDeviceAccount>
{
    public void Configure(EntityTypeBuilder<AuthLastActiveDeviceAccount> builder)
    {
        builder.ToTable("auth_last_active_device_accounts");

        builder.HasKey(item => item.DeviceId)
            .HasName("pk_auth_last_active_device_accounts");

        builder.Property(item => item.DeviceId)
            .HasColumnName("device_id");

        builder.Property(item => item.UserId)
            .HasColumnName("user_id");

        builder.Property(item => item.IsOfflineRestricted)
            .HasColumnName("is_offline_restricted")
            .IsRequired();

        builder.Property(item => item.MarkedAtUtc)
            .HasColumnName("marked_at_utc")
            .IsRequired();

        builder.HasOne(item => item.User)
            .WithMany(item => item.LastActiveDeviceAccounts)
            .HasForeignKey(item => item.UserId)
            .HasConstraintName("fk_auth_last_active_device_accounts_users_user_id");
    }
}