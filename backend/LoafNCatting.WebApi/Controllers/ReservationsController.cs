using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _reservationService.GetMineAsync(
            customerUserId,
            cancellationToken));
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{reservationId:int}")]
    public Task<IActionResult> GetMineById(
        int reservationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _reservationService.GetMineByIdAsync(
            customerUserId,
            reservationId,
            cancellationToken));
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(
            () => _reservationService.CreateAsync(
                customerUserId,
                request,
                cancellationToken),
            reservation => StatusCode(
                StatusCodes.Status201Created,
                reservation));
    }

    [Authorize(Roles = "Customer")]
    [HttpPatch("{reservationId:int}/cancel")]
    public Task<IActionResult> Cancel(
        int reservationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _reservationService.CancelByCustomerAsync(
            customerUserId,
            reservationId,
            cancellationToken));
    }

    [HttpGet("availability")]
    public Task<IActionResult> GetAvailability(
        [FromQuery] ReservationAvailabilityRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _reservationService.GetAvailabilityAsync(
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
