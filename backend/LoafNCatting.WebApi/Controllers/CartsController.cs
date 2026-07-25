using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.DTOs.Carts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/cart")]
public sealed class CartsController : ApiControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public Task<IActionResult> GetCart(CancellationToken cancellationToken)
        => WithCustomer(userId => _cartService.GetCartAsync(
            userId,
            cancellationToken));

    [HttpPost("items")]
    public Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
        => WithCustomer(userId => _cartService.AddItemAsync(
            userId,
            request,
            cancellationToken));

    [HttpPatch("items/{productId:int}")]
    public Task<IActionResult> UpdateItem(
        int productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
        => WithCustomer(userId => _cartService.UpdateItemAsync(
            userId,
            productId,
            request,
            cancellationToken));

    [HttpDelete("items/{productId:int}")]
    public Task<IActionResult> RemoveItem(
        int productId,
        CancellationToken cancellationToken)
        => WithCustomer(userId => _cartService.RemoveItemAsync(
            userId,
            productId,
            cancellationToken));

    [HttpDelete]
    public Task<IActionResult> ClearCart(CancellationToken cancellationToken)
        => WithCustomer(userId => _cartService.ClearAsync(
            userId,
            cancellationToken));

    private Task<IActionResult> WithCustomer<T>(Func<int, Task<T>> action)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(subject, out var customerUserId))
        {
            return Task.FromResult<IActionResult>(Error(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "The access token is missing a valid subject claim."));
        }

        return HandleAsync(() => action(customerUserId));
    }
}
