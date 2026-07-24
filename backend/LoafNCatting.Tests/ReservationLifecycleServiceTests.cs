using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationLifecycleServiceTests
{
    private static readonly DateOnly ReservationDate = new(2026, 7, 22);

    [TestMethod]
    public async Task ProcessDueReservationsAsync_ReservesAvailableTableForConfirmedReservationAtStartTime()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.ConfirmedStatusId,
            ReservationTestData.AvailableTableStatusId);
        var service = CreateService(data, hour: 9, minute: 0);

        var result = await service.ProcessDueReservationsAsync();

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        var reservation = await data.DbContext.Reservations.SingleAsync();
        Assert.AreEqual(1, result.TablesReserved);
        Assert.AreEqual(ReservationTestData.ReservedTableStatusId, table.TableStatusId);
        Assert.AreEqual(ReservationTestData.ConfirmedStatusId, reservation.StatusId);
        Assert.HasCount(0, result.Conflicts);
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_ExpiresPendingAtStartWithoutReservingTable()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.PendingStatusId,
            ReservationTestData.AvailableTableStatusId);
        var service = CreateService(data, hour: 9, minute: 0);

        var result = await service.ProcessDueReservationsAsync();

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        var reservation = await data.DbContext.Reservations.SingleAsync();
        Assert.AreEqual(1, result.ReservationsMarkedExpired);
        Assert.AreEqual(0, result.TablesReserved);
        Assert.AreEqual(0, result.TablesReleased);
        Assert.AreEqual(ReservationTestData.ExpiredStatusId, reservation.StatusId);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(10, notification.UserId);
        Assert.AreEqual(NotificationTypes.ReservationExpired, notification.Type);
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_KeepsPendingBeforeStartTime()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.PendingStatusId,
            ReservationTestData.AvailableTableStatusId);
        var service = CreateService(data, hour: 8, minute: 59);

        var result = await service.ProcessDueReservationsAsync();

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        var reservation = await data.DbContext.Reservations.SingleAsync();
        Assert.AreEqual(ReservationTestData.PendingStatusId, reservation.StatusId);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        Assert.AreEqual(0, result.TablesReserved);
        Assert.AreEqual(0, result.ReservationsMarkedExpired);
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_MarksConfirmedNoShowAfterHoldWindow()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.ConfirmedStatusId,
            ReservationTestData.ReservedTableStatusId);
        var service = CreateService(data, hour: 9, minute: 31);

        var result = await service.ProcessDueReservationsAsync();

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        var reservation = await data.DbContext.Reservations.SingleAsync();
        Assert.AreEqual(1, result.ReservationsMarkedNoShow);
        Assert.AreEqual(1, result.TablesReleased);
        Assert.AreEqual(ReservationTestData.NoShowStatusId, reservation.StatusId);
        Assert.AreEqual(ReservationTestData.AvailableTableStatusId, table.TableStatusId);
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(10, notification.UserId);
        Assert.AreEqual(NotificationTypes.ReservationNoShow, notification.Type);
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_DoesNotAutoCompleteCheckedInReservation()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.CheckedInStatusId,
            ReservationTestData.OccupiedTableStatusId);
        var service = CreateService(data, hour: 11, minute: 0);

        var result = await service.ProcessDueReservationsAsync();

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        var reservation = await data.DbContext.Reservations.SingleAsync();
        Assert.AreEqual(ReservationTestData.CheckedInStatusId, reservation.StatusId);
        Assert.AreEqual(ReservationTestData.OccupiedTableStatusId, table.TableStatusId);
        Assert.AreEqual(0, result.TablesReleased);
        Assert.AreEqual(0, result.ReservationsMarkedNoShow);
        Assert.AreEqual(0, result.ReservationsMarkedExpired);
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_ReportsCheckedInReservationTenMinutesBeforeEnd()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.CheckedInStatusId,
            ReservationTestData.OccupiedTableStatusId);
        var service = CreateService(data, hour: 10, minute: 50);

        var result = await service.ProcessDueReservationsAsync();
        await service.ProcessDueReservationsAsync();

        CollectionAssert.AreEqual(
            new[] { 1 },
            result.EndingSoonReservationIds.ToArray());
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(20, notification.UserId);
        Assert.AreEqual(NotificationTypes.ReservationEndingSoon, notification.Type);
        StringAssert.Contains(notification.Content, "Reservation #1");
    }

    [TestMethod]
    [DataRow(ReservationTestData.OccupiedTableStatusId)]
    [DataRow(ReservationTestData.MaintenanceTableStatusId)]
    public async Task ProcessDueReservationsAsync_DoesNotOverwriteUnavailablePhysicalTable(
        int currentTableStatusId)
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.ConfirmedStatusId,
            currentTableStatusId);
        var service = CreateService(data, hour: 9, minute: 0);

        var result = await service.ProcessDueReservationsAsync();
        var secondResult = await service.ProcessDueReservationsAsync();

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        Assert.AreEqual(currentTableStatusId, table.TableStatusId);
        Assert.HasCount(1, result.Conflicts);
        Assert.HasCount(1, secondResult.Conflicts);
        Assert.AreEqual(0, result.TablesReserved);
        var notification = await data.DbContext.Notifications.AsNoTracking().SingleAsync();
        Assert.AreEqual(20, notification.UserId);
        Assert.AreEqual(
            NotificationTypes.ReservationLifecycleConflict,
            notification.Type);
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_IsIdempotentAfterPendingExpires()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.PendingStatusId,
            ReservationTestData.AvailableTableStatusId);
        var service = CreateService(data, hour: 9, minute: 0);

        var firstResult = await service.ProcessDueReservationsAsync();
        var secondResult = await service.ProcessDueReservationsAsync();

        Assert.AreEqual(1, firstResult.ReservationsMarkedExpired);
        Assert.AreEqual(0, secondResult.ReservationsMarkedExpired);
        Assert.AreEqual(0, secondResult.TablesReleased);
        Assert.HasCount(0, secondResult.Conflicts);
        Assert.AreEqual(
            1,
            await data.DbContext.Notifications.CountAsync(notification =>
                notification.Type == NotificationTypes.ReservationExpired));
    }

    [TestMethod]
    public async Task ProcessDueReservationsAsync_WhenCustomerNotificationFails_RollsBackExpiration()
    {
        await using var data = await CreateScenarioAsync(
            ReservationTestData.PendingStatusId,
            ReservationTestData.AvailableTableStatusId);
        var clock = new TestTimeProvider(
            ReservationTestData.VietnamTime(2026, 7, 22, 9, 0));
        var service = new ReservationService(
            data.UnitOfWork,
            new ThrowingLifecycleNotificationService(),
            clock);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.ProcessDueReservationsAsync());

        Assert.AreEqual(
            ReservationTestData.PendingStatusId,
            await data.DbContext.Reservations
                .Select(reservation => reservation.StatusId)
                .SingleAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    private static async Task<TestDataContext> CreateScenarioAsync(
        int reservationStatusId,
        int tableStatusId)
    {
        var data = new TestDataContext();
        await data.SeedRolesAsync();
        await ReservationTestData.SeedStatusesAsync(data);
        AddUser(data, userId: 10, roleId: 3);
        AddUser(data, userId: 20, roleId: 2);
        ReservationTestData.AddTable(
            data,
            tableId: 1,
            capacity: 2,
            tableStatusId: tableStatusId);
        var reservation = ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            ReservationDate,
            new TimeOnly(9, 0),
            reservationStatusId);
        reservation.UserId = 10;
        await data.DbContext.SaveChangesAsync();
        data.DbContext.ChangeTracker.Clear();
        return data;
    }

    private static ReservationService CreateService(
        TestDataContext data,
        int hour,
        int minute)
    {
        var clock = new TestTimeProvider(
            ReservationTestData.VietnamTime(2026, 7, 22, hour, minute));
        return new ReservationService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            clock);
    }

    private static void AddUser(
        TestDataContext data,
        int userId,
        int roleId)
        => data.DbContext.Users.Add(new User
        {
            UserId = userId,
            Name = $"User {userId}",
            Email = $"user{userId}@example.com",
            Password = "hashed-password",
            PhoneNumber = $"090{userId:D7}",
            RoleId = roleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false
        });

    private sealed class ThrowingLifecycleNotificationService : INotificationService
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
            => throw new InvalidOperationException("Notification failure.");

        public Task<int> QueueForActiveStaffAsync(
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Notification failure.");

        public Task<int> QueueForActiveStaffIfMissingAsync(
            NotificationDraft draft,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Notification failure.");
    }
}
