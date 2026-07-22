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
    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(subject, out var customerUserId))
        {
            return Task.FromResult<IActionResult>(Error(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "The access token is missing a valid subject claim."));
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

    [HttpGet("availability")]
    public Task<IActionResult> GetAvailability(
        [FromQuery] ReservationAvailabilityRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _reservationService.GetAvailabilityAsync(
            request,
            cancellationToken));
}
