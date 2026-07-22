using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IUserAccountService
{
    Task<UserDto> CreateCustomerAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateStaffAsync(
        CreateStaffRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> EnsureAdminExistsAsync(
        BootstrapAdminSettings settings,
        CancellationToken cancellationToken = default);
}
