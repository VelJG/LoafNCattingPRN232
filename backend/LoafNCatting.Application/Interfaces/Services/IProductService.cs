using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search);

    Task<ProductDto?> GetProductAsync(int id);
}
