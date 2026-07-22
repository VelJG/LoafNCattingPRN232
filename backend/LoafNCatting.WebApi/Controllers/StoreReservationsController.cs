using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Staff,Admin")]
[Route("api/store/reservations")]
public sealed class StoreReservationsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;

    public StoreReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _reservationService.GetForStoreAsync(
            operatorUserId,
            cancellationToken));
    }

    [HttpGet("{reservationId:int}")]
    public Task<IActionResult> GetById(
        int reservationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _reservationService.GetForStoreByIdAsync(
            operatorUserId,
            reservationId,
            cancellationToken));
    }

    [HttpPatch("{reservationId:int}/confirm")]
    public Task<IActionResult> Confirm(
        int reservationId,
        CancellationToken cancellationToken)
        => HandleTransitionAsync(
            reservationId,
            _reservationService.ConfirmByStoreAsync,
            cancellationToken);

    [HttpPatch("{reservationId:int}/cancel")]
    public Task<IActionResult> Cancel(
        int reservationId,
        CancellationToken cancellationToken)
        => HandleTransitionAsync(
            reservationId,
            _reservationService.CancelByStoreAsync,
            cancellationToken);

    [HttpPatch("{reservationId:int}/check-in")]
    public Task<IActionResult> CheckIn(
        int reservationId,
        CancellationToken cancellationToken)
        => HandleTransitionAsync(
            reservationId,
            _reservationService.CheckInByStoreAsync,
            cancellationToken);

    [HttpPatch("{reservationId:int}/complete")]
    public Task<IActionResult> Complete(
        int reservationId,
        CancellationToken cancellationToken)
        => HandleTransitionAsync(
            reservationId,
            _reservationService.CompleteByStoreAsync,
            cancellationToken);

    private Task<IActionResult> HandleTransitionAsync(
        int reservationId,
        Func<int, int, CancellationToken, Task<StoreReservationDto>> transition,
        CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => transition(
            operatorUserId,
            reservationId,
            cancellationToken));
    }

    private bool TryGetOperatorUserId(out int operatorUserId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out operatorUserId);
    }

    private Task<IActionResult> InvalidSubject()
        => Task.FromResult<IActionResult>(Error(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "The access token is missing a valid subject claim."));
}
