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
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _cartService.GetCartAsync(
            customerUserId,
            cancellationToken));
    }

    [HttpPost("items")]
    public Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _cartService.AddItemAsync(
            customerUserId,
            request,
            cancellationToken));
    }

    [HttpPatch("items/{productId:int}")]
    public Task<IActionResult> UpdateItem(
        int productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _cartService.UpdateItemAsync(
            customerUserId,
            productId,
            request,
            cancellationToken));
    }

    [HttpDelete("items/{productId:int}")]
    public Task<IActionResult> RemoveItem(
        int productId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _cartService.RemoveItemAsync(
            customerUserId,
            productId,
            cancellationToken));
    }

    [HttpDelete]
    public Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _cartService.ClearAsync(
            customerUserId,
            cancellationToken));
    }

    private bool TryGetCustomerUserId(out int customerUserId)
        => int.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out customerUserId);

    private Task<IActionResult> InvalidSubject()
        => Task.FromResult<IActionResult>(Error(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "The access token is missing a valid subject claim."));
}
