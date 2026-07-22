using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface INotificationRealtimePublisher
{
    Task PublishChangedAsync(
        int recipientUserId,
        NotificationChangedRealtimeDto notificationEvent,
        CancellationToken cancellationToken = default);
}
