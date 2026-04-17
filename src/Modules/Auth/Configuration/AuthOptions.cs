namespace Modules.Auth.Configuration;

public sealed class AuthOptions
{
    public int AccessTokenLifetimeMinutes { get; init; } = 60;
    public int RefreshTokenLifetimeHours { get; init; } = 24;
    public bool OfflineRestrictedModeEnabled { get; init; } = true;

    public string DevelopmentBootstrapUserLogin { get; init; } = "platform-owner";
    public string DevelopmentBootstrapUserDisplayName { get; init; } = "Platform Owner";
    public string DevelopmentBootstrapUserPassword { get; init; } = "ChangeMe123!";
    public string DevelopmentBootstrapRoleCode { get; init; } = "platform_owner";
}