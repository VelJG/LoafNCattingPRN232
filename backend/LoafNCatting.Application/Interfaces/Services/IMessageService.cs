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
}
