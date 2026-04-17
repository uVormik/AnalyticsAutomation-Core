using BuildingBlocks.Contracts.Auth;

namespace Modules.Auth.Services;

public interface IAuthService
{
    Task<SignInResponseDto?> SignInAsync(
        SignInRequestDto request,
        CancellationToken cancellationToken);

    Task<RefreshSessionResponseDto?> RefreshAsync(
        RefreshSessionRequestDto request,
        CancellationToken cancellationToken);
}