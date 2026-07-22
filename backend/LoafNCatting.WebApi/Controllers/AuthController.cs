using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _authService.RegisterCustomerAsync(request, cancellationToken),
            user => Created("/api/auth/verify", user));

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(() => _authService.LoginAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("verify")]
    public Task<IActionResult> Verify(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var expiration = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (!int.TryParse(subject, out var userId) ||
            !long.TryParse(expiration, out var expirationSeconds))
        {
            return Task.FromResult<IActionResult>(Error(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "The access token is missing required claims."));
        }

        var expiresAtUtc = DateTimeOffset
            .FromUnixTimeSeconds(expirationSeconds)
            .UtcDateTime;
        return HandleAsync(() => _authService.VerifyAsync(
            userId,
            expiresAtUtc,
            cancellationToken));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();
}
