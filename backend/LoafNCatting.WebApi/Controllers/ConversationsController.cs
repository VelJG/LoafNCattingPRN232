using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/conversations")]
public sealed class ConversationsController : ApiControllerBase
{
    private readonly IMessageService _messageService;

    public ConversationsController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet("mine")]
    public Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _messageService.GetMineAsync(
            customerUserId,
            cancellationToken));
    }

    [HttpGet("mine/messages")]
    public Task<IActionResult> GetMineMessages(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _messageService.GetMineMessagesAsync(
            customerUserId,
            cancellationToken));
    }

    [HttpPost("mine/messages")]
    public Task<IActionResult> Send(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(
            () => _messageService.SendByCustomerAsync(
                customerUserId,
                request,
                cancellationToken),
            message => StatusCode(StatusCodes.Status201Created, message));
    }

    [HttpPatch("mine/messages/read")]
    public Task<IActionResult> MarkMineAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerUserId(out var customerUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => _messageService.MarkMineAsReadAsync(
            customerUserId,
            cancellationToken));
    }

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
