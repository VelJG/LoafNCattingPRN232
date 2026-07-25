using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IStoreLocationService
{
    Task<StoreLocationDto?> GetPrimaryLocationAsync(CancellationToken cancellationToken = default);
}
