using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class AdminProductService : IAdminProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorageService _mediaStorage;

    public AdminProductService(
        IUnitOfWork unitOfWork,
        IMediaStorageService? mediaStorage = null)
    {
        _unitOfWork = unitOfWork;
        _mediaStorage = mediaStorage ?? PassThroughMediaStorageService.Instance;
    }

    public async Task<IReadOnlyList<AdminProductDto>> GetAllAsync(string? search, int? categoryId, bool? isAvailable)
    {
        var query = _unitOfWork.Repository<Product>().Entities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(term) ||
                product.Description != null && product.Description.Contains(term));
        }

        if (categoryId is not null)
        {
            query = query.Where(product => product.CategoryId == categoryId);
        }

        if (isAvailable is not null)
        {
            query = query.Where(product => product.IsAvailable == isAvailable);
        }

        var items = await query
            .OrderBy(product => product.Name)
            .Select(product => new AdminProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                UnitInStock = product.UnitInStock,
                Picture = product.Picture,
                PictureKey = product.Picture,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                IsAvailable = product.IsAvailable,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            })
            .ToListAsync();

        foreach (var item in items)
        {
            item.PictureKey = _mediaStorage.NormalizeStoredKey(item.PictureKey);
            item.Picture = _mediaStorage.ResolveDisplayUrl(item.Picture);
        }

        return items;
    }

    public async Task<AdminProductDto?> GetByIdAsync(int productId)
    {
        var product = await _unitOfWork.Repository<Product>().Entities
            .AsNoTracking()
            .Where(product => product.ProductId == productId)
            .Select(product => new AdminProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                UnitInStock = product.UnitInStock,
                Picture = product.Picture,
                PictureKey = product.Picture,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                IsAvailable = product.IsAvailable,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            })
            .FirstOrDefaultAsync();
        if (product is null)
        {
            return null;
        }

        product.PictureKey = _mediaStorage.NormalizeStoredKey(product.PictureKey);
        product.Picture = _mediaStorage.ResolveDisplayUrl(product.Picture);
        return product;
    }

    public async Task<AdminProductDto> CreateAsync(AdminProductUpsertRequest request)
    {
        await ValidateAsync(request);

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            UnitInStock = request.UnitInStock,
            Picture = request.Picture,
            CategoryId = request.CategoryId,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Product>().InsertAsync(product, saveChanges: false);
        await _unitOfWork.SaveChangesAsync();

        return (await GetByIdAsync(product.ProductId))!;
    }

    public async Task<AdminProductDto?> UpdateAsync(int productId, AdminProductUpsertRequest request)
    {
        await ValidateAsync(request);

        var product = await _unitOfWork.Repository<Product>().FindAsync(productId);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.UnitInStock = request.UnitInStock;
        product.Picture = request.Picture;
        product.CategoryId = request.CategoryId;
        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(productId);
    }

    public async Task<bool> DeleteAsync(int productId)
    {
        var product = await _unitOfWork.Repository<Product>().FindAsync(productId);
        if (product is null)
        {
            return false;
        }

        await _unitOfWork.Repository<Product>().DeleteAsync(product, saveChanges: false);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task ValidateAsync(AdminProductUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("Price must be greater than or equal to 0.");
        }

        if (request.DiscountPrice is < 0)
        {
            throw new ArgumentException("Discount price must be greater than or equal to 0.");
        }

        if (request.DiscountPrice is not null && request.DiscountPrice > request.Price)
        {
            throw new ArgumentException("Discount price cannot be greater than price.");
        }

        if (request.UnitInStock < 0)
        {
            throw new ArgumentException("Unit in stock must be greater than or equal to 0.");
        }

        var categoryExists = await _unitOfWork.Repository<Category>().Entities
            .AsNoTracking()
            .AnyAsync(category => category.CategoryId == request.CategoryId);

        if (!categoryExists)
        {
            throw new ArgumentException("Category does not exist.");
        }
    }

}


