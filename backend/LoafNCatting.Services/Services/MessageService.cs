using System.Data;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class MessageService : IMessageService
{
    private const string CustomerRoleName = "Customer";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public MessageService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    public async Task<CustomerConversationDto> GetMineAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var conversation = await _unitOfWork.Repository<Conversation>()
            .Entities
            .AsNoTracking()
            .Where(conversation => conversation.CustomerUserId == customerUserId)
            .Select(conversation => new ConversationDto(
                conversation.ConversationId,
                conversation.CustomerUserId,
                conversation.CustomerUser.Name,
                conversation.CreatedAt,
                conversation.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return new CustomerConversationDto(conversation);
    }

    public async Task<IReadOnlyList<MessageDto>> GetMineMessagesAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        return await _unitOfWork.Repository<Message>()
            .Entities
            .AsNoTracking()
            .Where(message =>
                message.Conversation.CustomerUserId == customerUserId)
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.MessageId)
            .Select(message => new MessageDto(
                message.MessageId,
                message.ConversationId,
                message.SenderUserId,
                message.SenderUser.Name,
                message.SenderUser.Role.RoleName,
                message.Content,
                message.SentAt,
                message.IsRead))
            .ToListAsync(cancellationToken);
    }

    public async Task<MessageDto> SendByCustomerAsync(
        int customerUserId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);
        ArgumentNullException.ThrowIfNull(request);
        var content = string.IsNullOrWhiteSpace(request.Content)
            ? throw new ArgumentException("Message content is required.", nameof(request))
            : request.Content.Trim();
        var sentAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var customer = await GetActiveCustomerAsync(
                customerUserId,
                cancellationToken);
            var conversation = await _unitOfWork.Repository<Conversation>()
                .Entities
                .SingleOrDefaultAsync(
                    current => current.CustomerUserId == customerUserId,
                    cancellationToken);
            var isNewConversation = conversation is null;
            if (conversation is null)
            {
                conversation = new Conversation
                {
                    CustomerUserId = customerUserId,
                    StaffUserId = null,
                    CreatedAt = sentAtUtc,
                    UpdatedAt = sentAtUtc
                };
                await _unitOfWork.Repository<Conversation>().InsertAsync(
                    conversation,
                    saveChanges: false);
            }
            else
            {
                conversation.UpdatedAt = sentAtUtc;
            }

            var message = new Message
            {
                ConversationId = conversation.ConversationId,
                SenderUserId = customerUserId,
                Content = content,
                SentAt = sentAtUtc,
                IsRead = false
            };
            if (isNewConversation)
            {
                message.Conversation = conversation;
            }

            await _unitOfWork.Repository<Message>().InsertAsync(
                message,
                saveChanges: false);
            await _notificationService.QueueForActiveStaffAsync(
                new NotificationDraft(
                    "New customer message",
                    $"{customer.Name} sent a new message to the store.",
                    NotificationTypes.NewCustomerMessage),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new MessageDto(
                message.MessageId,
                conversation.ConversationId,
                customerUserId,
                customer.Name,
                CustomerRoleName,
                content,
                sentAtUtc,
                false);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task EnsureActiveCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken)
        => _ = await GetActiveCustomerAsync(customerUserId, cancellationToken);

    private async Task<User> GetActiveCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user =>
                    user.UserId == customerUserId &&
                    user.IsActive &&
                    user.Role.RoleName == CustomerRoleName,
                cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "The authenticated customer account is not active or valid.");

    private static void ValidateCustomerUserId(int customerUserId)
    {
        if (customerUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerUserId),
                "Customer user id must be greater than zero.");
        }
    }
}
