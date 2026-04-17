namespace BuildingBlocks.Infrastructure.Persistence.Entities.Auth;

public sealed class AuthUser
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string NormalizedLogin { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? CurrentGroupNodeId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastSignInAtUtc { get; set; }

    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();
    public ICollection<AuthSession> Sessions { get; set; } = new List<AuthSession>();
    public ICollection<AuthLastActiveDeviceAccount> LastActiveDeviceAccounts { get; set; } = new List<AuthLastActiveDeviceAccount>();
}

public sealed class AuthRole
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();
    public ICollection<AuthRolePermission> RolePermissions { get; set; } = new List<AuthRolePermission>();
}

public sealed class AuthPermission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<AuthRolePermission> RolePermissions { get; set; } = new List<AuthRolePermission>();
}

public sealed class AuthUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public AuthUser User { get; set; } = null!;
    public AuthRole Role { get; set; } = null!;
}

public sealed class AuthRolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public AuthRole Role { get; set; } = null!;
    public AuthPermission Permission { get; set; } = null!;
}

public sealed class AuthSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public string AccessTokenHash { get; set; } = string.Empty;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public bool IsOfflineRestricted { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset RefreshExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }

    public AuthUser User { get; set; } = null!;
}

public sealed class AuthLastActiveDeviceAccount
{
    public Guid DeviceId { get; set; }
    public Guid UserId { get; set; }
    public bool IsOfflineRestricted { get; set; }
    public DateTimeOffset MarkedAtUtc { get; set; }

    public AuthUser User { get; set; } = null!;
}