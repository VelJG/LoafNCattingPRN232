using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationCustomerServiceTests
{
    private static readonly DateOnly BookingDate = new(2026, 7, 22);

    [TestMethod]
    public async Task GetMineAsync_ReturnsOnlyOwnedReservationsNewestFirst()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, customerUserId: 10, BookingDate, new TimeOnly(9, 0));
        AddReservation(data, 2, customerUserId: 10, BookingDate.AddDays(1), new TimeOnly(8, 30));
        AddReservation(data, 3, customerUserId: 11, BookingDate.AddDays(2), new TimeOnly(10, 0));
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data).GetMineAsync(10);

        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            result.Select(reservation => reservation.ReservationId).ToArray());
        Assert.IsTrue(result.All(reservation => reservation.CustomerUserId == 10));
    }

    [TestMethod]
    public async Task GetMineByIdAsync_WhenOwned_ReturnsReservationDetails()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, customerUserId: 10, BookingDate, new TimeOnly(9, 0));
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data).GetMineByIdAsync(10, 1);

        Assert.AreEqual(1, result.ReservationId);
        Assert.AreEqual(10, result.CustomerUserId);
        Assert.AreEqual("T1", result.Table.TableName);
        Assert.AreEqual(90, result.DurationMinutes);
    }

    [TestMethod]
    public async Task GetMineByIdAsync_WhenOwnedByAnotherCustomer_ReturnsNotFound()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, customerUserId: 11, BookingDate, new TimeOnly(9, 0));
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() =>
            CreateService(data).GetMineByIdAsync(10, 1));
    }

    [TestMethod]
    [DataRow(ReservationTestData.PendingStatusId)]
    [DataRow(ReservationTestData.ConfirmedStatusId)]
    public async Task CancelByCustomerAsync_CancelsAllowedStatusAndNotifiesEveryActiveStaff(
        int initialStatusId)
    {
        await using var data = await CreateDataAsync();
        AddReservation(
            data,
            reservationId: 1,
            customerUserId: 10,
            BookingDate,
            new TimeOnly(9, 0),
            initialStatusId);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data).CancelByCustomerAsync(10, 1);

        var stored = await data.DbContext.Reservations
            .AsNoTracking()
            .SingleAsync();
        var table = await data.DbContext.RestaurantTables
            .AsNoTracking()
            .SingleAsync();
        var notifications = await data.DbContext.Notifications
            .AsNoTracking()
            .OrderBy(notification => notification.UserId)
            .ToListAsync();
        Assert.AreEqual(ReservationTestData.CancelledStatusId, stored.StatusId);
        Assert.AreEqual("Đã hủy", result.Status);
        Assert.IsNotNull(stored.UpdatedAt);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        Assert.HasCount(2, notifications);
        CollectionAssert.AreEqual(
            new int?[] { 20, 21 },
            notifications.Select(notification => notification.UserId).ToArray());
        Assert.IsTrue(notifications.All(notification =>
            notification.Type == NotificationTypes.ReservationCancelled));
    }

    [TestMethod]
    public async Task CancelByCustomerAsync_ExactlyTwoHoursBeforeStart_IsAllowed()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, customerUserId: 10, BookingDate, new TimeOnly(9, 0));
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data).CancelByCustomerAsync(10, 1);

        Assert.AreEqual("Đã hủy", result.Status);
    }

    [TestMethod]
    public async Task CancelByCustomerAsync_LessThanTwoHoursBeforeStart_IsRejected()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, customerUserId: 10, BookingDate, new TimeOnly(8, 30));
        await data.DbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data).CancelByCustomerAsync(10, 1));

        StringAssert.Contains(exception.Message, "at least 2 hours");
        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.PendingStatusId, stored.StatusId);
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task CancelByCustomerAsync_TerminalReservation_IsRejected()
    {
        await using var data = await CreateDataAsync();
        AddReservation(
            data,
            reservationId: 1,
            customerUserId: 10,
            BookingDate,
            new TimeOnly(9, 0),
            ReservationTestData.CompletedStatusId);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data).CancelByCustomerAsync(10, 1));

        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task CancelByCustomerAsync_WhenNotificationFails_RollsBackStatusChange()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, customerUserId: 10, BookingDate, new TimeOnly(9, 0));
        await data.DbContext.SaveChangesAsync();
        var clock = CreateClock();
        var service = new ReservationService(
            data.UnitOfWork,
            new ThrowingNotificationService(),
            clock);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CancelByCustomerAsync(10, 1));

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.PendingStatusId, stored.StatusId);
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
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        return data;
    }

    private static Reservation AddReservation(
        TestDataContext data,
        int reservationId,
        int customerUserId,
        DateOnly date,
        TimeOnly time,
        int statusId = ReservationTestData.PendingStatusId)
    {
        var reservation = ReservationTestData.AddReservation(
            data,
            reservationId,
            tableId: 1,
            date,
            time,
            statusId);
        reservation.UserId = customerUserId;
        return reservation;
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
