using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.WebApi.Hubs;

[Authorize(Roles = "Customer,Staff,Admin")]
public sealed class MessageHub : Hub
{
    public const string Route = "/hubs/messages";

    private readonly IMessageService _messageService;

    public MessageHub(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetAuthenticatedUserId();
        var role = GetAuthenticatedRole();
        if (role == "Customer")
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                MessageHubGroups.Customer(userId),
                Context.ConnectionAborted);
        }
        else
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                MessageHubGroups.StoreStaff,
                Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    [Authorize(Roles = "Customer")]
    public Task<MessageDto> SendCustomerMessage(SendMessageRequest request)
        => _messageService.SendByCustomerAsync(
            GetAuthenticatedUserId(),
            request,
            Context.ConnectionAborted);

    [Authorize(Roles = "Staff,Admin")]
    public Task<MessageDto> SendStoreMessage(
        int conversationId,
        SendMessageRequest request)
        => _messageService.SendByStoreAsync(
            GetAuthenticatedUserId(),
            conversationId,
            request,
            Context.ConnectionAborted);

    [Authorize(Roles = "Customer")]
    public Task<MarkMessagesReadResultDto> MarkCustomerMessagesRead()
        => _messageService.MarkMineAsReadAsync(
            GetAuthenticatedUserId(),
            Context.ConnectionAborted);

    [Authorize(Roles = "Staff,Admin")]
    public Task<MarkMessagesReadResultDto> MarkStoreMessagesRead(
        int conversationId)
        => _messageService.MarkStoreMessagesAsReadAsync(
            GetAuthenticatedUserId(),
            conversationId,
            Context.ConnectionAborted);

    private int GetAuthenticatedUserId()
    {
        var subject = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out var userId) && userId > 0
            ? userId
            : throw new HubException(
                "The access token is missing a valid subject claim.");
    }

    private string GetAuthenticatedRole()
        => Context.User?.FindFirstValue(AuthClaimTypes.Role)
            ?? throw new HubException(
                "The access token is missing a valid role claim.");
}

public static class MessageHubGroups
{
    public const string StoreStaff = "store-staff";

    public static string Customer(int customerUserId)
        => $"customer:{customerUserId}";
}
