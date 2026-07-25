using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class StoreLocationService : IStoreLocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public StoreLocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StoreLocationDto?> GetPrimaryLocationAsync(CancellationToken cancellationToken = default)
    {
        var location = await _unitOfWork.Repository<StoreLocation>()
            .Entities
            .AsNoTracking()
            .OrderBy(item => item.StoreLocationId)
            .FirstOrDefaultAsync(cancellationToken);

        return location is null ? null : CafeDtoMapper.ToStoreLocationDto(location);
    }
}
