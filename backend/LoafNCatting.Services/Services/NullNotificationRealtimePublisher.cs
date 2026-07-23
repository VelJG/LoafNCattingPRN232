using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;

namespace LoafNCatting.Services.Services;

public sealed class NullNotificationRealtimePublisher
    : INotificationRealtimePublisher
{
    public Task PublishChangedAsync(
        int recipientUserId,
        NotificationChangedRealtimeDto notificationEvent,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
