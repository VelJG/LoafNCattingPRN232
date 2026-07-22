using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Staff,Admin")]
[Route("api/store/walk-ins")]
public sealed class StoreWalkInsController : ApiControllerBase
{
    private readonly IReservationService _reservationService;

    public StoreWalkInsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateWalkInRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(
            () => _reservationService.CreateWalkInAsync(
                operatorUserId,
                request,
                cancellationToken),
            reservation => StatusCode(
                StatusCodes.Status201Created,
                reservation));
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
