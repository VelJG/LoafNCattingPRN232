using LoafNCatting.Application.DTOs.Carts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<CartDto> AddItemAsync(
        int userId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CartDto> UpdateItemAsync(
        int userId,
        int productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CartDto> RemoveItemAsync(
        int userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<CartDto> ClearAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
