using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IAdminCatService
{
    Task<IReadOnlyList<AdminCatDto>> GetAllAsync(string? search, string? status, string? gender);

    Task<AdminCatDto?> GetByIdAsync(int catId);

    Task<AdminCatOptionsDto> GetOptionsAsync();

    Task<AdminCatDto> CreateAsync(AdminCatUpsertRequest request);

    Task<AdminCatDto?> UpdateAsync(int catId, AdminCatUpsertRequest request);

    Task<bool> DeleteAsync(int catId);
}
