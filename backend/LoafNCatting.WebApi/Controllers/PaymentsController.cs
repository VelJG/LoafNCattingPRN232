using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/payments")]
public sealed class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("links")]
    public Task<IActionResult> CreateLink(
        [FromBody] CreatePaymentLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _paymentService.CreatePaymentLinkAsync(
            customerUserId,
            request.OrderId,
            cancellationToken));
    }

    [HttpGet("{orderId:int}/status")]
    public Task<IActionResult> GetStatus(
        int orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _paymentService.GetPaymentStatusAsync(
            customerUserId,
            orderId,
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
