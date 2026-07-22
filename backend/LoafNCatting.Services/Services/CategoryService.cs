using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var items = await _unitOfWork.Repository<Category>()
            .Entities
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();

        return items.Select(CafeDtoMapper.ToCategoryDto).ToList();
    }

    public async Task<CategoryDto?> GetCategoryAsync(int id)
    {
        var category = await _unitOfWork.Repository<Category>()
            .Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.CategoryId == id);

        return category is null ? null : CafeDtoMapper.ToCategoryDto(category);
    }
}
