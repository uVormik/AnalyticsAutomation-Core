using System.ComponentModel.DataAnnotations;

namespace Modules.Auth.Configuration;

public sealed class AuthOptions
{
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    [Range(1, 720)]
    public int RefreshTokenLifetimeHours { get; init; } = 24;

    public bool OfflineRestrictedModeEnabled { get; init; } = true;

    [Required]
    public string DevelopmentBootstrapUserLogin { get; init; } = "platform-owner";

    [Required]
    public string DevelopmentBootstrapUserDisplayName { get; init; } = "Platform Owner";

    [Required]
    public string DevelopmentBootstrapUserPassword { get; init; } = "ChangeMe123!";

    [Required]
    public string DevelopmentBootstrapRoleCode { get; init; } = "platform_owner";
}