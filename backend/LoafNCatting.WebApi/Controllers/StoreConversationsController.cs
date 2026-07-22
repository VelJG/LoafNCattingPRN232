using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Staff,Admin")]
[Route("api/store/conversations")]
public sealed class StoreConversationsController : ApiControllerBase
{
    private readonly IMessageService _messageService;

    public StoreConversationsController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => WithOperator(operatorUserId =>
            _messageService.GetStoreConversationsAsync(
                operatorUserId,
                cancellationToken));

    [HttpGet("{conversationId:int}/messages")]
    public Task<IActionResult> GetMessages(
        int conversationId,
        CancellationToken cancellationToken)
        => WithOperator(operatorUserId =>
            _messageService.GetStoreMessagesAsync(
                operatorUserId,
                conversationId,
                cancellationToken));

    [HttpPost("{conversationId:int}/messages")]
    public Task<IActionResult> Send(
        int conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
        => WithOperator(
            operatorUserId => _messageService.SendByStoreAsync(
                operatorUserId,
                conversationId,
                request,
                cancellationToken),
            message => StatusCode(StatusCodes.Status201Created, message));

    [HttpPatch("{conversationId:int}/messages/read")]
    public Task<IActionResult> MarkAsRead(
        int conversationId,
        CancellationToken cancellationToken)
        => WithOperator(operatorUserId =>
            _messageService.MarkStoreMessagesAsReadAsync(
                operatorUserId,
                conversationId,
                cancellationToken));

    private Task<IActionResult> WithOperator<T>(Func<int, Task<T>> action)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(() => action(operatorUserId));
    }

    private Task<IActionResult> WithOperator<T>(
        Func<int, Task<T>> action,
        Func<T, IActionResult> onSuccess)
    {
        if (!TryGetOperatorUserId(out var operatorUserId))
        {
            return InvalidSubject();
        }

        return HandleAsync(
            () => action(operatorUserId),
            onSuccess);
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
