using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly IAdminUserService _userService;

    public AdminUsersController(IAdminUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
        => HandleAsync(() => _userService.GetAllAsync(
            search,
            role,
            isActive,
            cancellationToken));

    [HttpGet("options")]
    public Task<IActionResult> GetOptions(CancellationToken cancellationToken)
        => HandleAsync(() => _userService.GetOptionsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] AdminUserUpsertRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _userService.CreateAsync(request, cancellationToken),
            user => CreatedAtAction(nameof(GetById), new { id = user.UserId }, user));

    [HttpPost("staff")]
    public Task<IActionResult> CreateStaff(
        [FromBody] CreateStaffRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _userService.CreateAsync(new AdminUserUpsertRequest
            {
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Role = "Staff",
                IsActive = true,
                IsEmailVerified = false,
                Password = request.Password
            }, cancellationToken),
            user => StatusCode(StatusCodes.Status201Created, user));

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(
        int id,
        [FromBody] AdminUserUpsertRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _userService.UpdateAsync(
                id,
                request,
                CurrentUserId(),
                cancellationToken),
            user => user is null ? NotFound() : Ok(user));

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _userService.DeleteAsync(
                id,
                CurrentUserId(),
                cancellationToken),
            deleted => deleted ? NoContent() : NotFound());

    private int? CurrentUserId()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out var userId) ? userId : null;
    }
}