using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IAdminTableService
{
    Task<IReadOnlyList<AdminTableDto>> GetAllAsync(
        string? search,
        string? status,
        string? area,
        CancellationToken cancellationToken = default);

    Task<AdminTableDto?> GetByIdAsync(
        int tableId,
        CancellationToken cancellationToken = default);

    Task<AdminTableOptionsDto> GetOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminTableDto> CreateAsync(
        AdminTableUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminTableDto?> UpdateAsync(
        int tableId,
        AdminTableUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int tableId,
        CancellationToken cancellationToken = default);
}
