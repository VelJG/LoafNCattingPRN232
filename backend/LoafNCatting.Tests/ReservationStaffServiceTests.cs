using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationStaffServiceTests
{
    private static readonly DateOnly BookingDate = new(2026, 7, 22);

    [TestMethod]
    public async Task GetForStoreAsync_ReturnsAllReservationsIncludingGuestReservation()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, ReservationTestData.PendingStatusId, customerUserId: 10);
        AddReservation(
            data,
            2,
            ReservationTestData.ConfirmedStatusId,
            customerUserId: null,
            time: new TimeOnly(9, 30));
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, hour: 7).GetForStoreAsync(20);

        CollectionAssert.AreEqual(
            new[] { 2, 1 },
            result.Select(reservation => reservation.ReservationId).ToArray());
        Assert.IsNull(result[0].CustomerUserId);
        Assert.IsNull(result[0].CustomerName);
        Assert.AreEqual(10, result[1].CustomerUserId);
        Assert.AreEqual("User 10", result[1].CustomerName);
        Assert.AreEqual("Trống", result[1].TableStatus);
    }

    [TestMethod]
    public async Task GetForStoreAsync_WithCustomerIdentity_IsRejectedByService()
    {
        await using var data = await CreateDataAsync();

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            CreateService(data, hour: 7).GetForStoreAsync(10));
    }

    [TestMethod]
    public async Task GetForStoreAsync_WithActiveAdmin_IsAllowed()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, ReservationTestData.PendingStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, hour: 7).GetForStoreAsync(30);

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public async Task ConfirmByStoreAsync_ChangesPendingAndNotifiesCustomer()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, ReservationTestData.PendingStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, hour: 7).ConfirmByStoreAsync(20, 1);

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        var table = await data.DbContext.RestaurantTables.AsNoTracking().SingleAsync();
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.ConfirmedStatusId, stored.StatusId);
        Assert.AreEqual("Đã xác nhận", result.Status);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        Assert.AreEqual(10, notification.UserId);
        Assert.AreEqual(NotificationTypes.ReservationConfirmed, notification.Type);
    }

    [TestMethod]
    public async Task ConfirmByStoreAsync_AtStartTime_IsRejected()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, ReservationTestData.PendingStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, hour: 9).ConfirmByStoreAsync(20, 1));

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.PendingStatusId, stored.StatusId);
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    [DataRow(ReservationTestData.PendingStatusId)]
    [DataRow(ReservationTestData.ConfirmedStatusId)]
    public async Task CancelByStoreAsync_CancelsAllowedStatusAndNotifiesCustomer(
        int initialStatusId)
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, initialStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, hour: 7).CancelByStoreAsync(20, 1);

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.CancelledStatusId, stored.StatusId);
        Assert.AreEqual("Đã hủy", result.Status);
        Assert.AreEqual(10, notification.UserId);
        Assert.AreEqual(NotificationTypes.ReservationCancelled, notification.Type);
    }

    [TestMethod]
    public async Task CancelByStoreAsync_AfterStart_ReleasesReservedTable()
    {
        await using var data = await CreateDataAsync(
            tableStatusId: ReservationTestData.ReservedTableStatusId);
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        await CreateService(data, hour: 9, minute: 5).CancelByStoreAsync(20, 1);

        var table = await data.DbContext.RestaurantTables.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
    }

    [TestMethod]
    public async Task CheckInByStoreAsync_AtStart_ChangesReservationAndTableAtomically()
    {
        await using var data = await CreateDataAsync(
            tableStatusId: ReservationTestData.ReservedTableStatusId);
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, hour: 9).CheckInByStoreAsync(20, 1);

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        var table = await data.DbContext.RestaurantTables.AsNoTracking().SingleAsync();
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.CheckedInStatusId, stored.StatusId);
        Assert.AreEqual("Đã đến", result.Status);
        Assert.AreEqual(ReservationTestData.OccupiedTableStatusId, table.TableStatusId);
        Assert.AreEqual(NotificationTypes.ReservationCheckedIn, notification.Type);
    }

    [TestMethod]
    public async Task CheckInByStoreAsync_BeforeStart_IsRejected()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, hour: 8, minute: 59).CheckInByStoreAsync(20, 1));

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.ConfirmedStatusId, stored.StatusId);
    }

    [TestMethod]
    public async Task CheckInByStoreAsync_WhenHoldWindowElapsed_IsRejected()
    {
        await using var data = await CreateDataAsync(
            tableStatusId: ReservationTestData.ReservedTableStatusId);
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, hour: 9, minute: 30).CheckInByStoreAsync(20, 1));

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.ConfirmedStatusId, stored.StatusId);
    }

    [TestMethod]
    [DataRow(ReservationTestData.OccupiedTableStatusId)]
    [DataRow(ReservationTestData.MaintenanceTableStatusId)]
    public async Task CheckInByStoreAsync_DoesNotOverwriteUnavailableTable(
        int tableStatusId)
    {
        await using var data = await CreateDataAsync(tableStatusId);
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, hour: 9).CheckInByStoreAsync(20, 1));

        var table = await data.DbContext.RestaurantTables.AsNoTracking().SingleAsync();
        Assert.AreEqual(tableStatusId, table.TableStatusId);
    }

    [TestMethod]
    public async Task CompleteByStoreAsync_CompletesAndReleasesOccupiedTable()
    {
        await using var data = await CreateDataAsync(
            tableStatusId: ReservationTestData.OccupiedTableStatusId);
        AddReservation(data, 1, ReservationTestData.CheckedInStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, hour: 10).CompleteByStoreAsync(20, 1);

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        var table = await data.DbContext.RestaurantTables.AsNoTracking().SingleAsync();
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.CompletedStatusId, stored.StatusId);
        Assert.AreEqual("Hoàn thành", result.Status);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        Assert.AreEqual(NotificationTypes.ReservationCompleted, notification.Type);
    }

    [TestMethod]
    public async Task CompleteByStoreAsync_WhenNotCheckedIn_IsRejected()
    {
        await using var data = await CreateDataAsync();
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, hour: 10).CompleteByStoreAsync(20, 1));
    }

    [TestMethod]
    public async Task CheckInByStoreAsync_WhenNotificationFails_RollsBackReservationAndTable()
    {
        await using var data = await CreateDataAsync(
            tableStatusId: ReservationTestData.ReservedTableStatusId);
        AddReservation(data, 1, ReservationTestData.ConfirmedStatusId, customerUserId: 10);
        await data.DbContext.SaveChangesAsync();
        var service = new ReservationService(
            data.UnitOfWork,
            new ThrowingNotificationService(),
            CreateClock(hour: 9));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CheckInByStoreAsync(20, 1));

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        var table = await data.DbContext.RestaurantTables.AsNoTracking().SingleAsync();
        Assert.AreEqual(ReservationTestData.ConfirmedStatusId, stored.StatusId);
        Assert.AreEqual(ReservationTestData.ReservedTableStatusId, table.TableStatusId);
    }

    private static async Task<TestDataContext> CreateDataAsync(
        int tableStatusId = ReservationTestData.AvailableTableStatusId)
    {
        var data = new TestDataContext();
        await data.SeedRolesAsync();
        await ReservationTestData.SeedStatusesAsync(data);
        AddUser(data, userId: 10, roleId: 3, isActive: true);
        AddUser(data, userId: 11, roleId: 3, isActive: true);
        AddUser(data, userId: 20, roleId: 2, isActive: true);
        AddUser(data, userId: 21, roleId: 2, isActive: false);
        AddUser(data, userId: 30, roleId: 1, isActive: true);
        ReservationTestData.AddTable(
            data,
            tableId: 1,
            capacity: 2,
            tableStatusId);
        await data.DbContext.SaveChangesAsync();
        return data;
    }

    private static Reservation AddReservation(
        TestDataContext data,
        int reservationId,
        int statusId,
        int? customerUserId,
        TimeOnly? time = null)
    {
        var reservation = ReservationTestData.AddReservation(
            data,
            reservationId,
            tableId: 1,
            BookingDate,
            time ?? new TimeOnly(9, 0),
            statusId);
        reservation.UserId = customerUserId;
        return reservation;
    }

    private static ReservationService CreateService(
        TestDataContext data,
        int hour,
        int minute = 0)
    {
        var clock = CreateClock(hour, minute);
        return new ReservationService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            clock);
    }

    private static TestTimeProvider CreateClock(int hour, int minute = 0)
        => new(ReservationTestData.VietnamTime(2026, 7, 22, hour, minute));

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
