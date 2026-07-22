using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface ICatService
{
    Task<List<CatDto>> GetCatsAsync(string? search);

    Task<CatDto?> GetCatAsync(int id);
}
