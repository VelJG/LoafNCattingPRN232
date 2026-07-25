using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("checkout-options")]
    public Task<IActionResult> GetCheckoutOptions(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetCheckoutOptionsAsync(
            customerUserId,
            cancellationToken));
    }

    [HttpGet("mine")]
    public Task<IActionResult> GetMine(
        [FromQuery] int? statusId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetMineAsync(
            customerUserId,
            statusId,
            cancellationToken));
    }

    [HttpGet("{orderId:int}")]
    public Task<IActionResult> GetMineById(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetMineByIdAsync(
            customerUserId,
            orderId,
            cancellationToken));
    }

    [HttpPost("checkout")]
    public Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(
            () => _orderService.CheckoutAsync(
                customerUserId,
                request,
                cancellationToken),
            order => StatusCode(StatusCodes.Status201Created, order));
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
