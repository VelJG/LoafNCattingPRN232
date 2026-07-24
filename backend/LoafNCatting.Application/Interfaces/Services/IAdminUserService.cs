using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetAllAsync(
        string? search,
        string? role,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<AdminUserDto?> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<AdminUserOptionsDto> GetOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminUserDto> CreateAsync(
        AdminUserUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminUserDto?> UpdateAsync(
        int userId,
        AdminUserUpsertRequest request,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int userId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);
}
