using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;

namespace LoafNCatting.Services.Services;

public sealed class NullMessageRealtimePublisher : IMessageRealtimePublisher
{
    public Task PublishMessageCreatedAsync(
        MessageCreatedRealtimeDto messageEvent,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishMessagesReadAsync(
        int customerUserId,
        MessagesReadRealtimeDto readEvent,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
