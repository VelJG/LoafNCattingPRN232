using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    public Task<IActionResult> GetOrders(
        [FromQuery] int? userId,
        [FromQuery] int? statusId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _orderService.GetOrdersAsync(
            userId,
            statusId,
            cancellationToken));

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("{orderId:int}")]
    public Task<IActionResult> GetOrder(
        int orderId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _orderService.GetOrderAsync(orderId, cancellationToken));

    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public Task<IActionResult> GetMine(
        [FromQuery] int? statusId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetOrdersForCustomerAsync(
            customerUserId,
            statusId,
            cancellationToken));
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("mine/{orderId:int}")]
    public Task<IActionResult> GetMineById(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetOrderForCustomerAsync(
            customerUserId,
            orderId,
            cancellationToken));
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("checkout")]
    public Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.CheckoutAsync(
            customerUserId,
            request,
            cancellationToken));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPatch("{orderId:int}/status")]
    public Task<IActionResult> UpdateStatus(
        int orderId,
        [FromBody] OrderStatusUpdateRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _orderService.UpdateStatusAsync(
            orderId,
            request,
            cancellationToken));

    private bool TryGetCustomerUserId(out int customerUserId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out customerUserId);
    }

    private Task<IActionResult> InvalidSubject()
        => Task.FromResult<IActionResult>(Error(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "The access token is missing a valid subject claim."));
}
