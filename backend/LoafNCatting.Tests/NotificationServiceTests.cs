using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class NotificationServiceTests
{
    private static readonly DateTimeOffset NotificationTime =
        new(2026, 7, 22, 3, 15, 0, TimeSpan.Zero);

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

    private static NotificationService CreateService(TestDataContext data)
        => new(data.UnitOfWork, new TestTimeProvider(NotificationTime));

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
}
