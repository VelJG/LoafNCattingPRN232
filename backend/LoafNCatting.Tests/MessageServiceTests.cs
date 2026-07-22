using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class MessageServiceTests
{
    private static readonly DateTimeOffset MessageTime =
        new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task GetMineAsync_BeforeFirstMessage_ReturnsEmptyConversationAndHistory()
    {
        await using var data = await CreateDataAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var conversation = await service.GetMineAsync(10);
        var messages = await service.GetMineMessagesAsync(10);

        Assert.IsNull(conversation.Conversation);
        Assert.HasCount(0, messages);
        Assert.HasCount(0, data.DbContext.ChangeTracker.Entries().ToList());
    }

    [TestMethod]
    public async Task SendByCustomerAsync_FirstMessage_CreatesSharedConversationMessageAndStaffNotification()
    {
        await using var data = await CreateDataAsync();
        var service = CreateService(data);

        var result = await service.SendByCustomerAsync(
            10,
            new SendMessageRequest { Content = "  Hello store  " });

        Assert.AreEqual(10, result.SenderUserId);
        Assert.AreEqual("Customer 10", result.SenderName);
        Assert.AreEqual("Customer", result.SenderRole);
        Assert.AreEqual("Hello store", result.Content);
        Assert.AreEqual(MessageTime.UtcDateTime, result.SentAtUtc);
        Assert.IsFalse(result.IsRead);

        var conversation = await data.DbContext.Conversations
            .AsNoTracking()
            .SingleAsync();
        Assert.AreEqual(10, conversation.CustomerUserId);
        Assert.IsNull(conversation.StaffUserId);
        Assert.AreEqual(MessageTime.UtcDateTime, conversation.CreatedAt);
        Assert.AreEqual(MessageTime.UtcDateTime, conversation.UpdatedAt);
        Assert.AreEqual(conversation.ConversationId, result.ConversationId);

        var message = await data.DbContext.Messages.AsNoTracking().SingleAsync();
        Assert.AreEqual(conversation.ConversationId, message.ConversationId);
        Assert.AreEqual(10, message.SenderUserId);
        Assert.AreEqual("Hello store", message.Content);
        Assert.IsFalse(message.IsRead);

        var notification = await data.DbContext.Notifications
            .AsNoTracking()
            .SingleAsync();
        Assert.AreEqual(20, notification.UserId);
        Assert.AreEqual(NotificationTypes.NewCustomerMessage, notification.Type);
        Assert.IsFalse(notification.IsRead);
    }

    [TestMethod]
    public async Task SendByCustomerAsync_SecondMessage_ReusesTheSameConversation()
    {
        await using var data = await CreateDataAsync();
        var clock = new TestTimeProvider(MessageTime);
        var service = CreateService(data, clock);

        var first = await service.SendByCustomerAsync(
            10,
            new SendMessageRequest { Content = "First" });
        clock.SetUtcNow(MessageTime.AddMinutes(2));
        var second = await service.SendByCustomerAsync(
            10,
            new SendMessageRequest { Content = "Second" });

        Assert.AreEqual(first.ConversationId, second.ConversationId);
        Assert.AreEqual(1, await data.DbContext.Conversations.CountAsync());
        Assert.AreEqual(2, await data.DbContext.Messages.CountAsync());
        Assert.AreEqual(
            MessageTime.AddMinutes(2).UtcDateTime,
            await data.DbContext.Conversations
                .Select(conversation => conversation.UpdatedAt)
                .SingleAsync());
    }

    [TestMethod]
    public async Task GetMineMessagesAsync_ReturnsOnlyOwnedMessagesInStableOrder()
    {
        await using var data = await CreateDataAsync();
        data.DbContext.Conversations.AddRange(
            Conversation(1, customerUserId: 10),
            Conversation(2, customerUserId: 11));
        data.DbContext.Messages.AddRange(
            Message(3, 1, senderUserId: 20, minute: 2, "Third"),
            Message(1, 1, senderUserId: 10, minute: 1, "First"),
            Message(2, 1, senderUserId: 20, minute: 2, "Second"),
            Message(4, 2, senderUserId: 11, minute: 0, "Other customer"));
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var result = await service.GetMineMessagesAsync(10);

        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 },
            result.Select(message => message.MessageId).ToArray());
        Assert.IsTrue(result.All(message => message.ConversationId == 1));
        Assert.HasCount(0, data.DbContext.ChangeTracker.Entries().ToList());
    }

    [TestMethod]
    public async Task SendByCustomerAsync_BlankContent_RejectsWithoutWritingAnything()
    {
        await using var data = await CreateDataAsync();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            service.SendByCustomerAsync(
                10,
                new SendMessageRequest { Content = "   " }));

        Assert.AreEqual(0, await data.DbContext.Conversations.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Messages.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task CustomerOperations_InactiveCustomer_ReturnUnauthorized()
    {
        await using var data = await CreateDataAsync();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.GetMineAsync(12));
        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.SendByCustomerAsync(
                12,
                new SendMessageRequest { Content = "Hello" }));

        Assert.AreEqual(0, await data.DbContext.Conversations.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Messages.CountAsync());
    }

    [TestMethod]
    public async Task SendByCustomerAsync_WhenNotificationFails_RollsBackConversationAndMessage()
    {
        await using var data = await CreateDataAsync();
        var service = new MessageService(
            data.UnitOfWork,
            new ThrowingNotificationService(),
            new NullMessageRealtimePublisher(),
            new TestTimeProvider(MessageTime),
            NullLogger<MessageService>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.SendByCustomerAsync(
                10,
                new SendMessageRequest { Content = "Hello" }));

        Assert.AreEqual(0, await data.DbContext.Conversations.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Messages.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task GetStoreConversationsAsync_ReturnsSharedInboxNewestFirstWithUnreadCount()
    {
        await using var data = await CreateDataAsync();
        data.DbContext.Conversations.AddRange(
            Conversation(1, customerUserId: 10),
            new Conversation
            {
                ConversationId = 2,
                CustomerUserId = 11,
                StaffUserId = null,
                CreatedAt = MessageTime.AddMinutes(1).UtcDateTime,
                UpdatedAt = MessageTime.AddMinutes(3).UtcDateTime
            });
        data.DbContext.Messages.AddRange(
            Message(1, 1, senderUserId: 10, minute: 1, "Customer message"),
            Message(2, 1, senderUserId: 20, minute: 2, "Store reply"),
            Message(3, 2, senderUserId: 11, minute: 3, "Newest message"));
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var result = await service.GetStoreConversationsAsync(20);

        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            result.Select(conversation => conversation.ConversationId).ToArray());
        Assert.AreEqual("Newest message", result[0].LastMessageContent);
        Assert.AreEqual(1, result[0].UnreadCustomerMessageCount);
        Assert.AreEqual("Store reply", result[1].LastMessageContent);
        Assert.AreEqual("Staff", result[1].LastMessageSenderRole);
        Assert.AreEqual(1, result[1].UnreadCustomerMessageCount);
        Assert.HasCount(0, data.DbContext.ChangeTracker.Entries().ToList());
    }

    [TestMethod]
    public async Task SendByStoreAsync_CreatesReplyCustomerNotificationAndRealtimeEvent()
    {
        await using var data = await CreateDataAsync();
        data.DbContext.Conversations.Add(Conversation(1, customerUserId: 10));
        await data.DbContext.SaveChangesAsync();
        var publisher = new RecordingMessageRealtimePublisher();
        var service = CreateService(data, publisher: publisher);

        var result = await service.SendByStoreAsync(
            20,
            1,
            new SendMessageRequest { Content = "  We can help  " });

        Assert.AreEqual(20, result.SenderUserId);
        Assert.AreEqual("Staff", result.SenderRole);
        Assert.AreEqual("We can help", result.Content);
        var message = await data.DbContext.Messages.AsNoTracking().SingleAsync();
        Assert.AreEqual(20, message.SenderUserId);
        Assert.IsFalse(message.IsRead);
        var notification = await data.DbContext.Notifications
            .AsNoTracking()
            .SingleAsync();
        Assert.AreEqual(10, notification.UserId);
        Assert.AreEqual(NotificationTypes.NewStaffReply, notification.Type);
        Assert.HasCount(1, publisher.MessageEvents);
        Assert.AreEqual(10, publisher.MessageEvents[0].CustomerUserId);
        Assert.AreEqual(result.MessageId, publisher.MessageEvents[0].Message.MessageId);
    }

    [TestMethod]
    public async Task MarkRead_UpdatesOnlyMessagesSentByTheOtherSide()
    {
        await using var data = await CreateDataAsync();
        data.DbContext.Conversations.Add(Conversation(1, customerUserId: 10));
        data.DbContext.Messages.AddRange(
            Message(1, 1, senderUserId: 10, minute: 1, "Customer message"),
            Message(2, 1, senderUserId: 20, minute: 2, "Store reply"));
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var publisher = new RecordingMessageRealtimePublisher();
        var service = CreateService(data, publisher: publisher);

        var staffRead = await service.MarkStoreMessagesAsReadAsync(20, 1);

        Assert.AreEqual(1, staffRead.UpdatedCount);
        Assert.IsTrue(await IsMessageReadAsync(data, 1));
        Assert.IsFalse(await IsMessageReadAsync(data, 2));

        var customerRead = await service.MarkMineAsReadAsync(10);

        Assert.AreEqual(1, customerRead.UpdatedCount);
        Assert.IsTrue(await IsMessageReadAsync(data, 2));
        Assert.HasCount(2, publisher.ReadEvents);
        Assert.AreEqual("Staff", publisher.ReadEvents[0].ReadEvent.ReaderRole);
        Assert.AreEqual("Customer", publisher.ReadEvents[1].ReadEvent.ReaderRole);
    }

    [TestMethod]
    public async Task StoreOperations_InvalidOperatorOrConversation_AreRejected()
    {
        await using var data = await CreateDataAsync();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.GetStoreConversationsAsync(10));
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() =>
            service.GetStoreMessagesAsync(20, 999));
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() =>
            service.SendByStoreAsync(
                20,
                999,
                new SendMessageRequest { Content = "Hello" }));
    }

    [TestMethod]
    public async Task SendByStoreAsync_WhenNotificationFails_RollsBackReplyAndTimestamp()
    {
        await using var data = await CreateDataAsync();
        var originalUpdatedAt = MessageTime.AddMinutes(-5).UtcDateTime;
        var conversation = Conversation(1, customerUserId: 10);
        conversation.UpdatedAt = originalUpdatedAt;
        data.DbContext.Conversations.Add(conversation);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = new MessageService(
            data.UnitOfWork,
            new ThrowingNotificationService(),
            new NullMessageRealtimePublisher(),
            new TestTimeProvider(MessageTime),
            NullLogger<MessageService>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.SendByStoreAsync(
                20,
                1,
                new SendMessageRequest { Content = "Hello" }));

        Assert.AreEqual(0, await data.DbContext.Messages.CountAsync());
        Assert.AreEqual(
            originalUpdatedAt,
            await data.DbContext.Conversations
                .Select(current => current.UpdatedAt)
                .SingleAsync());
    }

    [TestMethod]
    public async Task SendByCustomerAsync_WhenRealtimeFails_MessageRemainsCommitted()
    {
        await using var data = await CreateDataAsync();
        var clock = new TestTimeProvider(MessageTime);
        var service = new MessageService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            new ThrowingMessageRealtimePublisher(),
            clock,
            NullLogger<MessageService>.Instance);

        var result = await service.SendByCustomerAsync(
            10,
            new SendMessageRequest { Content = "Hello" });

        Assert.IsTrue(result.MessageId > 0);
        Assert.AreEqual(1, await data.DbContext.Messages.CountAsync());
        Assert.AreEqual(1, await data.DbContext.Notifications.CountAsync());
    }

    private static async Task<TestDataContext> CreateDataAsync()
    {
        var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 3, isActive: true);
        AddUser(data, userId: 12, roleId: 3, isActive: false);
        AddUser(data, userId: 20, roleId: 2, isActive: true);
        await data.DbContext.SaveChangesAsync();
        return data;
    }

    private static MessageService CreateService(
        TestDataContext data,
        TestTimeProvider? clock = null,
        IMessageRealtimePublisher? publisher = null)
    {
        clock ??= new TestTimeProvider(MessageTime);
        return new MessageService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            publisher ?? new NullMessageRealtimePublisher(),
            clock,
            NullLogger<MessageService>.Instance);
    }

    private static Task<bool> IsMessageReadAsync(
        TestDataContext data,
        int messageId)
        => data.DbContext.Messages
            .AsNoTracking()
            .Where(message => message.MessageId == messageId)
            .Select(message => message.IsRead)
            .SingleAsync();

    private static void AddUser(
        TestDataContext data,
        int userId,
        int roleId,
        bool isActive)
        => data.DbContext.Users.Add(new User
        {
            UserId = userId,
            Name = roleId == 3 ? $"Customer {userId}" : $"Staff {userId}",
            Email = $"user{userId}@example.com",
            Password = "hashed-password",
            PhoneNumber = $"090{userId:D7}",
            RoleId = roleId,
            IsActive = isActive,
            CreatedAt = MessageTime.UtcDateTime,
            IsEmailVerified = false
        });

    private static Conversation Conversation(int id, int customerUserId)
        => new()
        {
            ConversationId = id,
            CustomerUserId = customerUserId,
            StaffUserId = null,
            CreatedAt = MessageTime.UtcDateTime,
            UpdatedAt = MessageTime.UtcDateTime
        };

    private static Message Message(
        int id,
        int conversationId,
        int senderUserId,
        int minute,
        string content)
        => new()
        {
            MessageId = id,
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            Content = content,
            SentAt = MessageTime.AddMinutes(minute).UtcDateTime,
            IsRead = false
        };

    private sealed class ThrowingNotificationService : INotificationService
    {
        public Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
            int userId,
            bool? isRead = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UnreadNotificationCountDto> GetUnreadCountAsync(
            int userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NotificationDto> MarkAsReadAsync(
            int userId,
            int notificationId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MarkNotificationsReadResultDto> MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task QueueForUserAsync(
            int userId,
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Notification failure.");

        public Task<bool> QueueForUserIfMissingAsync(
            int userId,
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> QueueForActiveStaffAsync(
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Notification failure.");

        public Task<int> QueueForActiveStaffIfMissingAsync(
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingMessageRealtimePublisher : IMessageRealtimePublisher
    {
        public List<MessageCreatedRealtimeDto> MessageEvents { get; } = [];

        public List<(int CustomerUserId, MessagesReadRealtimeDto ReadEvent)> ReadEvents
            { get; } = [];

        public Task PublishMessageCreatedAsync(
            MessageCreatedRealtimeDto messageEvent,
            CancellationToken cancellationToken = default)
        {
            MessageEvents.Add(messageEvent);
            return Task.CompletedTask;
        }

        public Task PublishMessagesReadAsync(
            int customerUserId,
            MessagesReadRealtimeDto readEvent,
            CancellationToken cancellationToken = default)
        {
            ReadEvents.Add((customerUserId, readEvent));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingMessageRealtimePublisher : IMessageRealtimePublisher
    {
        public Task PublishMessageCreatedAsync(
            MessageCreatedRealtimeDto messageEvent,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Realtime failure.");

        public Task PublishMessagesReadAsync(
            int customerUserId,
            MessagesReadRealtimeDto readEvent,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Realtime failure.");
    }
}
