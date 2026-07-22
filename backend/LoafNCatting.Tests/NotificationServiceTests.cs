using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class NotificationServiceTests
{
    private static readonly DateTimeOffset NotificationTime =
        new(2026, 7, 22, 3, 15, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task GetForUserAsync_ReturnsOwnedNotificationsNewestFirstAndSupportsReadFilter()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 3, isActive: true);
        AddNotification(data, 1, 10, isRead: false, createdMinute: 1);
        AddNotification(data, 2, 10, isRead: true, createdMinute: 2);
        AddNotification(data, 3, 10, isRead: false, createdMinute: 3);
        AddNotification(data, 4, 11, isRead: false, createdMinute: 4);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var all = await service.GetForUserAsync(10);
        var unread = await service.GetForUserAsync(10, isRead: false);

        CollectionAssert.AreEqual(
            new[] { 3, 2, 1 },
            all.Select(notification => notification.NotificationId).ToArray());
        CollectionAssert.AreEqual(
            new[] { 3, 1 },
            unread.Select(notification => notification.NotificationId).ToArray());
        Assert.IsTrue(all.All(notification => notification.NotificationId != 4));
        Assert.HasCount(0, data.DbContext.ChangeTracker.Entries().ToList());
    }

    [TestMethod]
    public async Task GetUnreadCountAsync_CountsOnlyOwnedUnreadNotifications()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 2, isActive: true);
        AddNotification(data, 1, 10, isRead: false, createdMinute: 1);
        AddNotification(data, 2, 10, isRead: true, createdMinute: 2);
        AddNotification(data, 3, 11, isRead: false, createdMinute: 3);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var result = await service.GetUnreadCountAsync(10);

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task MarkAsReadAsync_OwnedNotification_PersistsReadState()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddNotification(data, 1, 10, isRead: false, createdMinute: 1);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var result = await service.MarkAsReadAsync(10, 1);

        Assert.IsTrue(result.IsRead);
        Assert.IsTrue(await data.DbContext.Notifications
            .Where(notification => notification.NotificationId == 1)
            .Select(notification => notification.IsRead)
            .SingleAsync());
    }

    [TestMethod]
    public async Task MarkAsReadAsync_AnotherUsersNotification_ReturnsNotFoundWithoutChangingIt()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 3, isActive: true);
        AddNotification(data, 1, 11, isRead: false, createdMinute: 1);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() =>
            service.MarkAsReadAsync(10, 1));

        Assert.IsFalse(await data.DbContext.Notifications
            .Where(notification => notification.NotificationId == 1)
            .Select(notification => notification.IsRead)
            .SingleAsync());
    }

    [TestMethod]
    public async Task MarkAllAsReadAsync_UpdatesOnlyOwnedUnreadNotificationsAndIsIdempotent()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 2, isActive: true);
        AddNotification(data, 1, 10, isRead: false, createdMinute: 1);
        AddNotification(data, 2, 10, isRead: true, createdMinute: 2);
        AddNotification(data, 3, 11, isRead: false, createdMinute: 3);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var first = await service.MarkAllAsReadAsync(10);
        var second = await service.MarkAllAsReadAsync(10);

        Assert.AreEqual(1, first.UpdatedCount);
        Assert.AreEqual(0, second.UpdatedCount);
        Assert.IsTrue(await data.DbContext.Notifications
            .Where(notification => notification.NotificationId == 1)
            .Select(notification => notification.IsRead)
            .SingleAsync());
        Assert.IsFalse(await data.DbContext.Notifications
            .Where(notification => notification.NotificationId == 3)
            .Select(notification => notification.IsRead)
            .SingleAsync());
    }

    [TestMethod]
    public async Task QueueForUserAsync_AddsUnreadNotificationWithoutCommitting()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        await service.QueueForUserAsync(
            10,
            new NotificationDraft(
                "  Reservation confirmed  ",
                "  Your reservation is confirmed.  ",
                $"  {NotificationTypes.ReservationConfirmed}  "));

        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
        var queued = data.DbContext.ChangeTracker
            .Entries<Notification>()
            .Single();
        Assert.AreEqual(EntityState.Added, queued.State);
        Assert.AreEqual(10, queued.Entity.UserId);
        Assert.AreEqual("Reservation confirmed", queued.Entity.Title);
        Assert.AreEqual(
            "Your reservation is confirmed.",
            queued.Entity.Content);
        Assert.AreEqual(
            NotificationTypes.ReservationConfirmed,
            queued.Entity.Type);
        Assert.IsFalse(queued.Entity.IsRead);
        Assert.AreEqual(NotificationTime.UtcDateTime, queued.Entity.CreatedAt);

        await data.UnitOfWork.SaveChangesAsync();
        Assert.AreEqual(1, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task QueueForUserIfMissingAsync_DoesNotDuplicatePersistedEvent()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);
        var draft = new NotificationDraft(
            "Reservation expired",
            "Reservation #42 expired.",
            NotificationTypes.ReservationExpired);

        var firstQueued = await service.QueueForUserIfMissingAsync(10, draft);
        await data.UnitOfWork.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var secondQueued = await service.QueueForUserIfMissingAsync(10, draft);
        await data.UnitOfWork.SaveChangesAsync();

        Assert.IsTrue(firstQueued);
        Assert.IsFalse(secondQueued);
        Assert.AreEqual(1, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task QueueForActiveStaffAsync_QueuesOneNotificationPerActiveStaffOnly()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 2, isActive: true);
        AddUser(data, userId: 11, roleId: 2, isActive: true);
        AddUser(data, userId: 12, roleId: 2, isActive: false);
        AddUser(data, userId: 13, roleId: 3, isActive: true);
        AddUser(data, userId: 14, roleId: 1, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var recipientCount = await service.QueueForActiveStaffAsync(
            new NotificationDraft(
                "New reservation",
                "A customer created a reservation.",
                NotificationTypes.ReservationCreated));
        await data.UnitOfWork.SaveChangesAsync();

        var notifications = await data.DbContext.Notifications
            .AsNoTracking()
            .OrderBy(notification => notification.UserId)
            .ToListAsync();
        Assert.AreEqual(2, recipientCount);
        Assert.HasCount(2, notifications);
        CollectionAssert.AreEqual(
            new int?[] { 10, 11 },
            notifications.Select(notification => notification.UserId).ToArray());
        Assert.IsTrue(notifications.All(notification => !notification.IsRead));
        Assert.IsTrue(notifications.All(notification =>
            notification.Type == NotificationTypes.ReservationCreated));
    }

    [TestMethod]
    public async Task QueueForActiveStaffIfMissingAsync_DoesNotDuplicateEventPerStaff()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 2, isActive: true);
        AddUser(data, userId: 11, roleId: 2, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);
        var draft = new NotificationDraft(
            "Reservation ending soon",
            "Reservation #42 at table A1 will end at 18:30.",
            NotificationTypes.ReservationEndingSoon);

        var firstCount = await service.QueueForActiveStaffIfMissingAsync(draft);
        await data.UnitOfWork.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var secondCount = await service.QueueForActiveStaffIfMissingAsync(draft);
        await data.UnitOfWork.SaveChangesAsync();

        Assert.AreEqual(2, firstCount);
        Assert.AreEqual(0, secondCount);
        Assert.AreEqual(2, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task QueueForActiveStaffAsync_WithNoActiveStaff_QueuesNothing()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 2, isActive: false);
        AddUser(data, userId: 11, roleId: 3, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        var recipientCount = await service.QueueForActiveStaffAsync(
            new NotificationDraft(
                "New reservation",
                "A customer created a reservation.",
                NotificationTypes.ReservationCreated));

        Assert.AreEqual(0, recipientCount);
        Assert.HasCount(
            0,
            data.DbContext.ChangeTracker.Entries<Notification>().ToList());
    }

    [TestMethod]
    public async Task QueueForUserAsync_RejectsInactiveRecipient()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: false);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() =>
            service.QueueForUserAsync(
                10,
                new NotificationDraft(
                    "Reservation update",
                    "Your reservation changed.",
                    NotificationTypes.ReservationConfirmed)));
        Assert.HasCount(
            0,
            data.DbContext.ChangeTracker.Entries<Notification>().ToList());
    }

    [TestMethod]
    [DataRow("", "Content", "Type")]
    [DataRow("Title", "   ", "Type")]
    [DataRow("Title", "Content", "")]
    public async Task QueueForUserAsync_RejectsBlankNotificationFields(
        string title,
        string content,
        string type)
    {
        await using var data = new TestDataContext();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            service.QueueForUserAsync(
                10,
                new NotificationDraft(title, content, type)));
    }

    [TestMethod]
    public async Task QueueForActiveStaffAsync_RejectsTitleLongerThanDatabaseLimit()
    {
        await using var data = new TestDataContext();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            service.QueueForActiveStaffAsync(
                new NotificationDraft(
                    new string('A', 256),
                    "Content",
                    NotificationTypes.ReservationCreated)));
    }

    [TestMethod]
    public async Task QueueForUserAsync_PublishesRealtimeOnlyAfterTransactionCommit()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var publisher = new RecordingNotificationRealtimePublisher();
        var service = CreateService(data, publisher);
        await data.UnitOfWork.BeginTransactionAsync();

        await service.QueueForUserAsync(
            10,
            new NotificationDraft(
                "New reply",
                "The store replied.",
                NotificationTypes.NewStaffReply));

        Assert.HasCount(0, publisher.Events);

        await data.UnitOfWork.CommitTransactionAsync();

        Assert.HasCount(1, publisher.Events);
        var notificationEvent = publisher.Events[0].NotificationEvent;
        Assert.AreEqual(NotificationRealtimeChangeTypes.Created, notificationEvent.ChangeType);
        Assert.IsNotNull(notificationEvent.Notification);
        Assert.IsTrue(notificationEvent.Notification.NotificationId > 0);
        Assert.AreEqual(1, notificationEvent.UnreadCount);
    }

    [TestMethod]
    public async Task QueueForUserAsync_Rollback_DoesNotPublishRealtime()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var publisher = new RecordingNotificationRealtimePublisher();
        var service = CreateService(data, publisher);
        await data.UnitOfWork.BeginTransactionAsync();

        await service.QueueForUserAsync(
            10,
            new NotificationDraft(
                "New reply",
                "The store replied.",
                NotificationTypes.NewStaffReply));
        await data.UnitOfWork.RollbackTransactionAsync();

        Assert.HasCount(0, publisher.Events);
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task MarkAsReadAsync_PublishesUpdatedUnreadCount()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddNotification(data, 1, 10, isRead: false, createdMinute: 1);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var publisher = new RecordingNotificationRealtimePublisher();
        var service = CreateService(data, publisher);

        await service.MarkAsReadAsync(10, 1);

        Assert.HasCount(1, publisher.Events);
        var notificationEvent = publisher.Events[0].NotificationEvent;
        Assert.AreEqual(NotificationRealtimeChangeTypes.Read, notificationEvent.ChangeType);
        Assert.AreEqual(0, notificationEvent.UnreadCount);
        Assert.IsTrue(notificationEvent.Notification!.IsRead);
    }

    [TestMethod]
    public async Task QueueForUserAsync_WhenRealtimeFails_NotificationRemainsCommitted()
    {
        await using var data = new TestDataContext();
        await data.SeedRolesAsync();
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        var service = CreateService(
            data,
            new ThrowingNotificationRealtimePublisher());
        await data.UnitOfWork.BeginTransactionAsync();

        await service.QueueForUserAsync(
            10,
            new NotificationDraft(
                "New reply",
                "The store replied.",
                NotificationTypes.NewStaffReply));
        await data.UnitOfWork.CommitTransactionAsync();

        Assert.AreEqual(1, await data.DbContext.Notifications.CountAsync());
    }

    private static NotificationService CreateService(TestDataContext data)
        => new(data.UnitOfWork, new TestTimeProvider(NotificationTime));

    private static NotificationService CreateService(
        TestDataContext data,
        INotificationRealtimePublisher publisher)
        => new(
            data.UnitOfWork,
            new TestTimeProvider(NotificationTime),
            publisher,
            NullLogger<NotificationService>.Instance);

    private static void AddUser(
        TestDataContext data,
        int userId,
        int roleId,
        bool isActive)
        => data.DbContext.Users.Add(new User
        {
            UserId = userId,
            Name = $"User {userId}",
            Email = $"user{userId}@example.com",
            Password = "hashed-password",
            PhoneNumber = $"090{userId:D7}",
            RoleId = roleId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false
        });

    private static void AddNotification(
        TestDataContext data,
        int notificationId,
        int userId,
        bool isRead,
        int createdMinute)
        => data.DbContext.Notifications.Add(new Notification
        {
            NotificationId = notificationId,
            UserId = userId,
            Title = $"Notification {notificationId}",
            Content = $"Content {notificationId}",
            Type = NotificationTypes.ReservationConfirmed,
            IsRead = isRead,
            CreatedAt = new DateTime(2026, 7, 22, 3, createdMinute, 0, DateTimeKind.Utc)
        });

    private sealed class RecordingNotificationRealtimePublisher
        : INotificationRealtimePublisher
    {
        public List<(int UserId, NotificationChangedRealtimeDto NotificationEvent)> Events
            { get; } = [];

        public Task PublishChangedAsync(
            int recipientUserId,
            NotificationChangedRealtimeDto notificationEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add((recipientUserId, notificationEvent));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingNotificationRealtimePublisher
        : INotificationRealtimePublisher
    {
        public Task PublishChangedAsync(
            int recipientUserId,
            NotificationChangedRealtimeDto notificationEvent,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Realtime failure.");
    }
}
