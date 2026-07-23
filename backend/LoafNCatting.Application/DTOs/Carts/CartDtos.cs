using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.DTOs.Carts;

public sealed record CartDto(
    int CartId,
    int UserId,
    IReadOnlyList<CartItemDto> Items,
    decimal Total);

public sealed record CartItemDto(
    int CartItemId,
    int ProductId,
    string ProductName,
    string? Picture,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record AddCartItemRequest(
    [property: Range(1, int.MaxValue)] int ProductId,
    [property: Range(1, int.MaxValue)] int Quantity);

public sealed record UpdateCartItemRequest(
    [property: Range(0, int.MaxValue)] int Quantity);
