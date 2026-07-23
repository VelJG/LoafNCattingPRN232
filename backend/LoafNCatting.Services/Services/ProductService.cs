using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorageService _mediaStorage;

    public ProductService(
        IUnitOfWork unitOfWork,
        IMediaStorageService? mediaStorage = null)
    {
        _unitOfWork = unitOfWork;
        _mediaStorage = mediaStorage ?? PassThroughMediaStorageService.Instance;
    }

    public async Task<List<ProductDto>> GetProductsAsync(int? categoryId, string? search)
    {
        var query = _unitOfWork.Repository<Product>()
            .Entities
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(keyword) ||
                (product.Description != null && product.Description.Contains(keyword)));
        }

        var items = await query
            .OrderBy(product => product.ProductId)
            .ToListAsync();

        return items.Select(product => CafeDtoMapper.ToProductDto(product, _mediaStorage)).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var product = await _unitOfWork.Repository<Product>()
            .Entities
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ProductId == id);

        return product is null ? null : CafeDtoMapper.ToProductDto(product, _mediaStorage);
    }
}
