using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class CatService : ICatService
{
    private readonly IUnitOfWork _unitOfWork;

    public CatService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CatDto>> GetCatsAsync(string? search)
    {
        var query = _unitOfWork.Repository<Cat>()
            .Entities
            .AsNoTracking()
            .Include(cat => cat.Gender)
            .Include(cat => cat.Status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(cat =>
                cat.Name.Contains(keyword) ||
                (cat.Breed != null && cat.Breed.Contains(keyword)) ||
                (cat.Description != null && cat.Description.Contains(keyword)));
        }

        var items = await query
            .OrderBy(cat => cat.CatId)
            .ToListAsync();

        return items.Select(CafeDtoMapper.ToCatDto).ToList();
    }

    public async Task<CatDto?> GetCatAsync(int id)
    {
        var cat = await _unitOfWork.Repository<Cat>()
            .Entities
            .AsNoTracking()
            .Include(item => item.Gender)
            .Include(item => item.Status)
            .FirstOrDefaultAsync(item => item.CatId == id);

        return cat is null ? null : CafeDtoMapper.ToCatDto(cat);
    }
}
