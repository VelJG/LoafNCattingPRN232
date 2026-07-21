using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IAdminProductService
{
    Task<IReadOnlyList<AdminProductDto>> GetAllAsync(string? search, int? categoryId, bool? isAvailable);

    Task<AdminProductDto?> GetByIdAsync(int productId);

    Task<AdminProductDto> CreateAsync(AdminProductUpsertRequest request);

    Task<AdminProductDto?> UpdateAsync(int productId, AdminProductUpsertRequest request);

    Task<bool> DeleteAsync(int productId);
}
