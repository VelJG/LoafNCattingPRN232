using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IAuthService
{
    Task<UserDto> RegisterCustomerAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<TokenVerificationResponse> VerifyAsync(
        int userId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
