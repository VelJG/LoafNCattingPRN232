using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
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

    [HttpGet("availability")]
    public Task<IActionResult> GetAvailability(
        [FromQuery] ReservationAvailabilityRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _reservationService.GetAvailabilityAsync(
            request,
            cancellationToken));
}
