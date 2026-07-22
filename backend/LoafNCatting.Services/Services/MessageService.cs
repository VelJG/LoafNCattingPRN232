using System.Data;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoafNCatting.Services.Services;

public sealed class MessageService : IMessageService
{
    private const string CustomerRoleName = "Customer";
    private const string StaffRoleName = "Staff";
    private const string AdminRoleName = "Admin";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IMessageRealtimePublisher _realtimePublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IMessageRealtimePublisher realtimePublisher,
        TimeProvider timeProvider,
        ILogger<MessageService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _realtimePublisher = realtimePublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CustomerConversationDto> GetMineAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var conversation = await _unitOfWork.Repository<Conversation>()
            .Entities
            .AsNoTracking()
            .Where(current => current.CustomerUserId == customerUserId)
            .Select(current => new ConversationDto(
                current.ConversationId,
                current.CustomerUserId,
                current.CustomerUser.Name,
                current.CreatedAt,
                current.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
        return new CustomerConversationDto(conversation);
    }

    public async Task<IReadOnlyList<MessageDto>> GetMineMessagesAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        return await ProjectMessages()
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
        ValidateUserId(customerUserId, nameof(customerUserId));
        var content = ValidateContent(request);
        var sentAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        MessageDto result;

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

            result = new MessageDto(
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

        await PublishMessageCreatedSafelyAsync(
            customerUserId,
            result);
        return result;
    }

    public async Task<MarkMessagesReadResultDto> MarkMineAsReadAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var conversationId = await _unitOfWork.Repository<Conversation>()
            .Entities
            .AsNoTracking()
            .Where(conversation => conversation.CustomerUserId == customerUserId)
            .Select(conversation => (int?)conversation.ConversationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (conversationId is null)
        {
            return new MarkMessagesReadResultDto(0);
        }

        var messages = await _unitOfWork.Repository<Message>()
            .Entities
            .Where(message =>
                message.ConversationId == conversationId.Value &&
                !message.IsRead &&
                message.SenderUser.Role.RoleName != CustomerRoleName)
            .ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        if (messages.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await PublishMessagesReadSafelyAsync(
                customerUserId,
                new MessagesReadRealtimeDto(
                    conversationId.Value,
                    customerUserId,
                    CustomerRoleName,
                    messages.Count));
        }

        return new MarkMessagesReadResultDto(messages.Count);
    }

    public async Task<IReadOnlyList<StoreConversationDto>> GetStoreConversationsAsync(
        int operatorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);

        return await _unitOfWork.Repository<Conversation>()
            .Entities
            .AsNoTracking()
            .OrderByDescending(conversation =>
                conversation.UpdatedAt ?? conversation.CreatedAt)
            .ThenByDescending(conversation => conversation.ConversationId)
            .Select(conversation => new StoreConversationDto(
                conversation.ConversationId,
                conversation.CustomerUserId,
                conversation.CustomerUser.Name,
                conversation.CreatedAt,
                conversation.UpdatedAt,
                conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.MessageId)
                    .Select(message => message.Content)
                    .FirstOrDefault(),
                conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.MessageId)
                    .Select(message => (DateTime?)message.SentAt)
                    .FirstOrDefault(),
                conversation.Messages
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.MessageId)
                    .Select(message => message.SenderUser.Role.RoleName)
                    .FirstOrDefault(),
                conversation.Messages.Count(message =>
                    !message.IsRead &&
                    message.SenderUser.Role.RoleName == CustomerRoleName)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> GetStoreMessagesAsync(
        int operatorUserId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        ValidateConversationId(conversationId);
        await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);
        await EnsureConversationExistsAsync(conversationId, cancellationToken);

        return await ProjectMessages()
            .Where(message => message.ConversationId == conversationId)
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

    public async Task<MessageDto> SendByStoreAsync(
        int operatorUserId,
        int conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        ValidateConversationId(conversationId);
        var content = ValidateContent(request);
        var sentAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        MessageDto result;
        int customerUserId;

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var storeOperator = await GetActiveStoreOperatorAsync(
                operatorUserId,
                cancellationToken);
            var conversation = await _unitOfWork.Repository<Conversation>()
                .Entities
                .Include(current => current.CustomerUser)
                .SingleOrDefaultAsync(
                    current => current.ConversationId == conversationId,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Conversation with id '{conversationId}' was not found.");
            customerUserId = conversation.CustomerUserId;
            conversation.UpdatedAt = sentAtUtc;

            var message = new Message
            {
                ConversationId = conversationId,
                SenderUserId = operatorUserId,
                Content = content,
                SentAt = sentAtUtc,
                IsRead = false
            };
            await _unitOfWork.Repository<Message>().InsertAsync(
                message,
                saveChanges: false);
            await _notificationService.QueueForUserAsync(
                customerUserId,
                new NotificationDraft(
                    "New store reply",
                    "The store replied to your conversation.",
                    NotificationTypes.NewStaffReply),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            result = new MessageDto(
                message.MessageId,
                conversationId,
                operatorUserId,
                storeOperator.Name,
                storeOperator.Role.RoleName,
                content,
                sentAtUtc,
                false);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        await PublishMessageCreatedSafelyAsync(
            customerUserId,
            result);
        return result;
    }

    public async Task<MarkMessagesReadResultDto> MarkStoreMessagesAsReadAsync(
        int operatorUserId,
        int conversationId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        ValidateConversationId(conversationId);
        var storeOperator = await GetActiveStoreOperatorAsync(
            operatorUserId,
            cancellationToken);
        var customerUserId = await _unitOfWork.Repository<Conversation>()
            .Entities
            .AsNoTracking()
            .Where(conversation => conversation.ConversationId == conversationId)
            .Select(conversation => (int?)conversation.CustomerUserId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Conversation with id '{conversationId}' was not found.");

        var messages = await _unitOfWork.Repository<Message>()
            .Entities
            .Where(message =>
                message.ConversationId == conversationId &&
                !message.IsRead &&
                message.SenderUser.Role.RoleName == CustomerRoleName)
            .ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.IsRead = true;
        }

        if (messages.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await PublishMessagesReadSafelyAsync(
                customerUserId,
                new MessagesReadRealtimeDto(
                    conversationId,
                    operatorUserId,
                    storeOperator.Role.RoleName,
                    messages.Count));
        }

        return new MarkMessagesReadResultDto(messages.Count);
    }

    private IQueryable<Message> ProjectMessages()
        => _unitOfWork.Repository<Message>()
            .Entities
            .AsNoTracking();

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
            .Include(user => user.Role)
            .SingleOrDefaultAsync(
                user =>
                    user.UserId == customerUserId &&
                    user.IsActive &&
                    user.Role.RoleName == CustomerRoleName,
                cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "The authenticated customer account is not active or valid.");

    private async Task EnsureActiveStoreOperatorAsync(
        int operatorUserId,
        CancellationToken cancellationToken)
        => _ = await GetActiveStoreOperatorAsync(operatorUserId, cancellationToken);

    private async Task<User> GetActiveStoreOperatorAsync(
        int operatorUserId,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(
                user =>
                    user.UserId == operatorUserId &&
                    user.IsActive &&
                    (user.Role.RoleName == StaffRoleName ||
                     user.Role.RoleName == AdminRoleName),
                cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "The authenticated store operator account is not active or valid.");

    private async Task EnsureConversationExistsAsync(
        int conversationId,
        CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Conversation>()
            .Entities
            .AsNoTracking()
            .AnyAsync(
                conversation => conversation.ConversationId == conversationId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                $"Conversation with id '{conversationId}' was not found.");
        }
    }

    private async Task PublishMessageCreatedSafelyAsync(
        int customerUserId,
        MessageDto message)
    {
        try
        {
            await _realtimePublisher.PublishMessageCreatedAsync(
                new MessageCreatedRealtimeDto(customerUserId, message),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Message {MessageId} was committed but its realtime event could not be published.",
                message.MessageId);
        }
    }

    private async Task PublishMessagesReadSafelyAsync(
        int customerUserId,
        MessagesReadRealtimeDto readEvent)
    {
        try
        {
            await _realtimePublisher.PublishMessagesReadAsync(
                customerUserId,
                readEvent,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Read state for Conversation {ConversationId} was committed but its realtime event could not be published.",
                readEvent.ConversationId);
        }
    }

    private static string ValidateContent(SendMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return string.IsNullOrWhiteSpace(request.Content)
            ? throw new ArgumentException(
                "Message content is required.",
                nameof(request))
            : request.Content.Trim();
    }

    private static void ValidateUserId(int userId, string parameterName)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "User id must be greater than zero.");
        }
    }

    private static void ValidateConversationId(int conversationId)
    {
        if (conversationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(conversationId),
                "Conversation id must be greater than zero.");
        }
    }
}
