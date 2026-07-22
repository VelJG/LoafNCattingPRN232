using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.WebApi.Services;

public sealed class SignalRNotificationRealtimePublisher
    : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimePublisher(
        IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishChangedAsync(
        int recipientUserId,
        NotificationChangedRealtimeDto notificationEvent,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group(NotificationHubGroups.User(recipientUserId))
            .SendAsync(
                NotificationRealtimeEvents.Changed,
                notificationEvent,
                cancellationToken);
}
