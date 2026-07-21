using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Application.Interfaces.Services;
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

    [HttpGet]
    public Task<IActionResult> GetOrders(
        [FromQuery] int? userId,
        [FromQuery] int? statusId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _orderService.GetOrdersAsync(
            userId,
            statusId,
            cancellationToken));

    [HttpGet("{orderId:int}")]
    public Task<IActionResult> GetOrder(
        int orderId,
        CancellationToken cancellationToken)
        => HandleAsync(() => _orderService.GetOrderAsync(orderId, cancellationToken));

    [HttpPost("checkout")]
    public Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _orderService.CheckoutAsync(request, cancellationToken));

    [HttpPatch("{orderId:int}/status")]
    public Task<IActionResult> UpdateStatus(
        int orderId,
        [FromBody] OrderStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAdminOrStaff())
        {
            return Task.FromResult<IActionResult>(Forbid());
        }

        return HandleAsync(() => _orderService.UpdateStatusAsync(
            orderId,
            request,
            cancellationToken));
    }

    private bool IsAdminOrStaff()
    {
        var role = Request.Headers["X-Role"].ToString();
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);
    }
}
