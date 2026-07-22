using LoafNCatting.Application.DTOs.Carts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Route("api/users/{userId:int}/cart")]
public sealed class CartsController : ApiControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public Task<IActionResult> GetCart(
        int userId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _cartService.GetCartAsync(userId, cancellationToken));

    [HttpPost("items")]
    public Task<IActionResult> AddItem(
        int userId,
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _cartService.AddItemAsync(userId, request, cancellationToken));

    [HttpPatch("items/{productId:int}")]
    public Task<IActionResult> UpdateItem(
        int userId,
        int productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _cartService.UpdateItemAsync(
            userId,
            productId,
            request,
            cancellationToken));

    [HttpDelete("items/{productId:int}")]
    public Task<IActionResult> RemoveItem(
        int userId,
        int productId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _cartService.RemoveItemAsync(
            userId,
            productId,
            cancellationToken));

    [HttpDelete]
    public Task<IActionResult> ClearCart(
        int userId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _cartService.ClearAsync(userId, cancellationToken));
}
