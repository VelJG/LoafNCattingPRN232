using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationCreateServiceTests
{
    private static readonly DateOnly BookingDate = new(2026, 7, 22);

    [TestMethod]
    public async Task CreateAsync_CreatesPendingOnSmallestTableAndNotifiesEveryActiveStaff()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 3, capacity: 4);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.CreateAsync(
            customerUserId: 10,
            Request(hour: 8, minute: 30, guests: 2));

        var reservation = await data.DbContext.Reservations
            .AsNoTracking()
            .SingleAsync();
        var table = await data.DbContext.RestaurantTables
            .AsNoTracking()
            .SingleAsync(current => current.TableId == 1);
        var notifications = await data.DbContext.Notifications
            .AsNoTracking()
            .OrderBy(notification => notification.UserId)
            .ToListAsync();

        Assert.AreEqual(10, result.CustomerUserId);
        Assert.AreEqual(ReservationTestData.PendingStatusId, reservation.StatusId);
        Assert.AreEqual("Đang chờ", result.Status);
        Assert.AreEqual(1, reservation.TableId);
        Assert.AreEqual(1, result.Table.TableId);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        Assert.AreEqual("Bill", reservation.GuestName);
        Assert.AreEqual("0987654321", reservation.GuestPhoneNumber);
        Assert.AreEqual("Near a quiet area", reservation.Note);
        Assert.HasCount(2, notifications);
        CollectionAssert.AreEqual(
            new int?[] { 20, 21 },
            notifications.Select(notification => notification.UserId).ToArray());
        Assert.IsTrue(notifications.All(notification =>
            notification.Type == NotificationTypes.ReservationCreated));
    }

    [TestMethod]
    public async Task CreateAsync_RejectsOverlappingReservationForSameCustomer()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);
        await service.CreateAsync(10, Request(8, 30, guests: 2));

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateAsync(10, Request(9, 0, guests: 2)));

        StringAssert.Contains(exception.Message, "overlapping active reservation");
        Assert.AreEqual(1, await data.DbContext.Reservations.CountAsync());
    }

    [TestMethod]
    public async Task CreateAsync_AllowsDifferentCustomersAtSameTimeWhenAnotherTableExists()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var first = await service.CreateAsync(10, Request(8, 30, guests: 2));
        var second = await service.CreateAsync(11, Request(8, 30, guests: 2));

        Assert.AreEqual(1, first.Table.TableId);
        Assert.AreEqual(2, second.Table.TableId);
        Assert.AreEqual(2, await data.DbContext.Reservations.CountAsync());
    }

    [TestMethod]
    public async Task CreateAsync_WhenNoSuitableTableExists_RollsBackWithoutNotification()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateAsync(10, Request(8, 30, guests: 6)));

        Assert.AreEqual(0, await data.DbContext.Reservations.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task CreateAsync_RejectsIdentityThatIsNotAnActiveCustomer()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(20, Request(8, 30, guests: 2)));

        Assert.AreEqual(0, await data.DbContext.Reservations.CountAsync());
    }

    [TestMethod]
    public async Task CreateAsync_WhenNotificationFails_RollsBackReservation()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var clock = CreateClock();
        var service = new ReservationService(
            data.UnitOfWork,
            new ThrowingNotificationService(),
            clock);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateAsync(10, Request(8, 30, guests: 2)));

        Assert.AreEqual(0, await data.DbContext.Reservations.CountAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    private static async Task<TestDataContext> CreateDataAsync()
    {
        var data = new TestDataContext();
        await data.SeedRolesAsync();
        await ReservationTestData.SeedStatusesAsync(data);
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 3, isActive: true);
        AddUser(data, userId: 20, roleId: 2, isActive: true);
        AddUser(data, userId: 21, roleId: 2, isActive: true);
        AddUser(data, userId: 22, roleId: 2, isActive: false);
        await data.DbContext.SaveChangesAsync();
        return data;
    }

    private static ReservationService CreateService(TestDataContext data)
    {
        var clock = CreateClock();
        return new ReservationService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            clock);
    }

    private static TestTimeProvider CreateClock()
        => new(ReservationTestData.VietnamTime(2026, 7, 22, 7, 0));

    private static CreateReservationRequest Request(int hour, int minute, int guests)
        => new()
        {
            Date = BookingDate,
            Time = new TimeOnly(hour, minute),
            NumberOfGuests = guests,
            GuestName = "  Bill  ",
            GuestPhoneNumber = "  0987654321  ",
            Note = "  Near a quiet area  "
        };

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

        public Task<int> QueueForActiveStaffAsync(
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Notification failure.");
    }
}
