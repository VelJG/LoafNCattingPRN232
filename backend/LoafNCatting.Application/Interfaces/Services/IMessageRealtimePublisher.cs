using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IMessageRealtimePublisher
{
    Task PublishMessageCreatedAsync(
        MessageCreatedRealtimeDto messageEvent,
        CancellationToken cancellationToken = default);

    Task PublishMessagesReadAsync(
        int customerUserId,
        MessagesReadRealtimeDto readEvent,
        CancellationToken cancellationToken = default);
}
