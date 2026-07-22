using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

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
            new TestTimeProvider(MessageTime));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.SendByCustomerAsync(
                10,
                new SendMessageRequest { Content = "Hello" }));

        Assert.AreEqual(0, await data.DbContext.Conversations.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Messages.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
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
        TestTimeProvider? clock = null)
    {
        clock ??= new TestTimeProvider(MessageTime);
        return new MessageService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            clock);
    }

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
            => throw new NotSupportedException();

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
}
