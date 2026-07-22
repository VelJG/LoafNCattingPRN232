using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IMessageService
{
    Task<CustomerConversationDto> GetMineAsync(
        int customerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> GetMineMessagesAsync(
        int customerUserId,
        CancellationToken cancellationToken = default);

    Task<MessageDto> SendByCustomerAsync(
        int customerUserId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<MarkMessagesReadResultDto> MarkMineAsReadAsync(
        int customerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreConversationDto>> GetStoreConversationsAsync(
        int operatorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> GetStoreMessagesAsync(
        int operatorUserId,
        int conversationId,
        CancellationToken cancellationToken = default);

    Task<MessageDto> SendByStoreAsync(
        int operatorUserId,
        int conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<MarkMessagesReadResultDto> MarkStoreMessagesAsReadAsync(
        int operatorUserId,
        int conversationId,
        CancellationToken cancellationToken = default);
}
