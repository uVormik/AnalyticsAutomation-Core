using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using BuildingBlocks.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Modules.Auth.Authentication;

public sealed class OpaqueBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var accessToken = authorizationHeader[BearerPrefix.Length..].Trim();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return AuthenticateResult.Fail("Bearer token was empty.");
        }

        var accessTokenHash = ComputeHash(accessToken);

        PlatformDbContext dbContext = Context.RequestServices.GetRequiredService<PlatformDbContext>();

        var session = await dbContext.AuthSessions
            .AsNoTracking()
            .Include(item => item.User)
                .ThenInclude(item => item.UserRoles)
                    .ThenInclude(item => item.Role)
                        .ThenInclude(item => item.RolePermissions)
                            .ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(
                item => item.AccessTokenHash == accessTokenHash,
                Context.RequestAborted);

        var now = DateTimeOffset.UtcNow;

        if (session is null
            || session.RevokedAtUtc is not null
            || session.ExpiresAtUtc <= now
            || !session.User.IsActive)
        {
            return AuthenticateResult.Fail("Bearer token was invalid or expired.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString("D")),
            new(ClaimTypes.Name, session.User.Login),
            new("session_id", session.Id.ToString("D"))
        };

        if (session.DeviceId.HasValue)
        {
            claims.Add(new Claim("device_id", session.DeviceId.Value.ToString("D")));
        }

        if (session.User.CurrentGroupNodeId.HasValue)
        {
            claims.Add(new Claim("current_group_node_id", session.User.CurrentGroupNodeId.Value.ToString("D")));
        }

        foreach (var roleCode in session.User.UserRoles
            .Select(item => item.Role.Code)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
            claims.Add(new Claim("role", roleCode));
        }

        foreach (var permissionCode in session.User.UserRoles
            .SelectMany(item => item.Role.RolePermissions)
            .Select(item => item.Permission.Code)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("permission", permissionCode));
        }

        var identity = new ClaimsIdentity(claims, AuthAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthAuthenticationDefaults.Scheme);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeHash(string value)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));
    }
}