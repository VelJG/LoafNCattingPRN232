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
        var service = CreateService(data, hour: 10, minute: 20);

        var result = await service.ProcessDueReservationsAsync();

        CollectionAssert.AreEqual(
            new[] { 1 },
            result.EndingSoonReservationIds.ToArray());
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

        var table = await data.DbContext.RestaurantTables.SingleAsync();
        Assert.AreEqual(currentTableStatusId, table.TableStatusId);
        Assert.HasCount(1, result.Conflicts);
        Assert.AreEqual(0, result.TablesReserved);
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
    }

    private static async Task<TestDataContext> CreateScenarioAsync(
        int reservationStatusId,
        int tableStatusId)
    {
        var data = new TestDataContext();
        await ReservationTestData.SeedStatusesAsync(data);
        ReservationTestData.AddTable(
            data,
            tableId: 1,
            capacity: 2,
            tableStatusId: tableStatusId);
        ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            ReservationDate,
            new TimeOnly(9, 0),
            reservationStatusId);
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
}
