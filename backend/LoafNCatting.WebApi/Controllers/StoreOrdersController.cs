using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Staff,Admin")]
[Route("api/store/orders")]
public sealed class StoreOrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public StoreOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] int? statusId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetForStoreAsync(
            operatorUserId,
            statusId,
            cancellationToken));
    }

    [HttpGet("{orderId:int}")]
    public Task<IActionResult> GetById(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.GetForStoreByIdAsync(
            operatorUserId,
            orderId,
            cancellationToken));
    }

    [HttpPatch("{orderId:int}/status")]
    public Task<IActionResult> UpdateStatus(
        int orderId,
        [FromBody] OrderStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _orderService.UpdateStatusByStoreAsync(
            operatorUserId,
            orderId,
            request,
            cancellationToken));
    }

    private bool TryGetOperatorUserId(out int operatorUserId)
        => int.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out operatorUserId);

    private Task<IActionResult> InvalidSubject()
        => Task.FromResult<IActionResult>(Error(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "The access token is missing a valid subject claim."));
}
