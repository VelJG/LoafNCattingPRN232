using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.WebApi.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Route = "/hubs/notifications";

    public override async Task OnConnectedAsync()
    {
        var subject = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(subject, out var userId) || userId <= 0)
        {
            throw new HubException(
                "The access token is missing a valid subject claim.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            NotificationHubGroups.User(userId),
            Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}

public static class NotificationHubGroups
{
    public static string User(int userId) => $"notification-user:{userId}";
}
