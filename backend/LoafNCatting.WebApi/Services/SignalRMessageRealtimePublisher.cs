using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LoafNCatting.WebApi.Services;

public sealed class SignalRMessageRealtimePublisher : IMessageRealtimePublisher
{
    private readonly IHubContext<MessageHub> _hubContext;

    public SignalRMessageRealtimePublisher(IHubContext<MessageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishMessageCreatedAsync(
        MessageCreatedRealtimeDto messageEvent,
        CancellationToken cancellationToken = default)
        => Task.WhenAll(
            _hubContext.Clients
                .Group(MessageHubGroups.StoreStaff)
                .SendAsync(
                    MessageRealtimeEvents.MessageCreated,
                    messageEvent,
                    cancellationToken),
            _hubContext.Clients
                .Group(MessageHubGroups.Customer(messageEvent.CustomerUserId))
                .SendAsync(
                    MessageRealtimeEvents.MessageCreated,
                    messageEvent,
                    cancellationToken));

    public Task PublishMessagesReadAsync(
        int customerUserId,
        MessagesReadRealtimeDto readEvent,
        CancellationToken cancellationToken = default)
        => Task.WhenAll(
            _hubContext.Clients
                .Group(MessageHubGroups.StoreStaff)
                .SendAsync(
                    MessageRealtimeEvents.MessagesRead,
                    readEvent,
                    cancellationToken),
            _hubContext.Clients
                .Group(MessageHubGroups.Customer(customerUserId))
                .SendAsync(
                    MessageRealtimeEvents.MessagesRead,
                    readEvent,
                    cancellationToken));
}
