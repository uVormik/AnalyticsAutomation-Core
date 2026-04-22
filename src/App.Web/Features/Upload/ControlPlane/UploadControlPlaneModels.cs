namespace App.Web.Features.Upload.ControlPlane;

public sealed class UploadControlPlaneSignInRequest
{
    public string Login { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed class UploadControlPlaneRefreshRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class UploadControlPlaneUserSummary
{
    public Guid? UserId { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public sealed class UploadControlPlaneSignInResponse
{
    public string? AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public Guid? UserId { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    public UploadControlPlaneUserSummary? User { get; init; }

    public Guid? EffectiveUserId => User?.UserId ?? UserId;

    public string? EffectiveDisplayName => User?.DisplayName ?? DisplayName;
}

public sealed class UploadControlPlaneGroupNode
{
    public Guid? Id { get; init; }

    public Guid? ParentId { get; init; }

    public string? Name { get; init; }

    public string? Code { get; init; }

    public string? Path { get; init; }

    public bool? IsSelectable { get; init; }
}

public sealed record UploadControlPlaneSession(
    Guid? UserId,
    string? DisplayName,
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset CreatedAtUtc)
{
    public UploadControlPlaneSanitizedSession ToSanitized() =>
        new(
            UserId,
            DisplayName,
            HasAccessToken: !string.IsNullOrWhiteSpace(AccessToken),
            HasRefreshToken: !string.IsNullOrWhiteSpace(RefreshToken),
            CreatedAtUtc);
}

public sealed record UploadControlPlaneSanitizedSession(
    Guid? UserId,
    string? DisplayName,
    bool HasAccessToken,
    bool HasRefreshToken,
    DateTimeOffset CreatedAtUtc);