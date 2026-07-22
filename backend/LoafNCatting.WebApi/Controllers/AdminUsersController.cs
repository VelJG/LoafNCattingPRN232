using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly IUserAccountService _userAccountService;

    public AdminUsersController(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [HttpPost("staff")]
    public Task<IActionResult> CreateStaff(
        [FromBody] CreateStaffRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _userAccountService.CreateStaffAsync(request, cancellationToken),
            user => Created($"/api/admin/users/staff/{user.UserId}", user));
}
