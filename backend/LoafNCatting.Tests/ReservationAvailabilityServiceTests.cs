using LoafNCatting.Application.Contracts;
using LoafNCatting.Services.Services;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationAvailabilityServiceTests
{
    private static readonly DateOnly BookingDate = new(2026, 7, 22);

    [TestMethod]
    public async Task GetAvailabilityAsync_SelectsSmallestTableThenLowestTableId()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 3, capacity: 4);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.GetAvailabilityAsync(Request(8, 30, guests: 2));

        Assert.IsTrue(result.IsAvailable);
        Assert.IsNotNull(result.SuggestedTable);
        Assert.AreEqual(1, result.SuggestedTable.TableId);
        Assert.AreEqual(2, result.SuggestedTable.Capacity);
        Assert.AreEqual(90, result.DurationMinutes);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_UsesAnotherTableWhenFirstTableOverlaps()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 2);
        ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            BookingDate,
            new TimeOnly(8, 30),
            ReservationTestData.PendingStatusId);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.GetAvailabilityAsync(Request(9, 0, guests: 2));

        Assert.IsTrue(result.IsAvailable);
        Assert.IsNotNull(result.SuggestedTable);
        Assert.AreEqual(2, result.SuggestedTable.TableId);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_AllowsSameTableAtPreviousReservationEnd()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            BookingDate,
            new TimeOnly(8, 30),
            ReservationTestData.ConfirmedStatusId);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.GetAvailabilityAsync(Request(10, 0, guests: 2));

        Assert.IsTrue(result.IsAvailable);
        Assert.IsNotNull(result.SuggestedTable);
        Assert.AreEqual(1, result.SuggestedTable.TableId);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_ReturnsUnavailableWhenEverySuitableTableOverlaps()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            BookingDate,
            new TimeOnly(8, 30),
            ReservationTestData.CheckedInStatusId);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.GetAvailabilityAsync(Request(9, 30, guests: 2));

        Assert.IsFalse(result.IsAvailable);
        Assert.IsNull(result.SuggestedTable);
        Assert.IsNotNull(result.Reason);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_IgnoresTerminalReservations()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        var terminalStatuses = new[]
        {
            ReservationTestData.CancelledStatusId,
            ReservationTestData.CompletedStatusId,
            ReservationTestData.NoShowStatusId,
            ReservationTestData.ExpiredStatusId
        };
        for (var index = 0; index < terminalStatuses.Length; index++)
        {
            ReservationTestData.AddReservation(
                data,
                reservationId: index + 1,
                tableId: 1,
                BookingDate,
                new TimeOnly(8, 30),
                terminalStatuses[index]);
        }

        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.GetAvailabilityAsync(Request(9, 0, guests: 2));

        Assert.IsTrue(result.IsAvailable);
        Assert.IsNotNull(result.SuggestedTable);
        Assert.AreEqual(1, result.SuggestedTable.TableId);
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_ExcludesMaintenanceTables()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(
            data,
            tableId: 1,
            capacity: 2,
            tableStatusId: ReservationTestData.MaintenanceTableStatusId);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 4);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        var result = await service.GetAvailabilityAsync(Request(8, 30, guests: 2));

        Assert.IsNotNull(result.SuggestedTable);
        Assert.AreEqual(2, result.SuggestedTable.TableId);
    }

    [TestMethod]
    [DataRow(8, 0)]
    [DataRow(9, 15)]
    [DataRow(21, 0)]
    public async Task GetAvailabilityAsync_RejectsInvalidBookingSlots(int hour, int minute)
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var service = CreateService(data);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            service.GetAvailabilityAsync(Request(hour, minute, guests: 2)));
    }

    [TestMethod]
    public async Task GetAvailabilityAsync_RejectsBookingLessThanThirtyMinutesAhead()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var clock = new TestTimeProvider(
            ReservationTestData.VietnamTime(2026, 7, 22, 8, 1));
        var service = new ReservationService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            clock);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            service.GetAvailabilityAsync(Request(8, 30, guests: 2)));
    }

    private static async Task<TestDataContext> CreateDataAsync()
    {
        var data = new TestDataContext();
        await ReservationTestData.SeedStatusesAsync(data);
        return data;
    }

    private static ReservationService CreateService(TestDataContext data)
    {
        var clock = new TestTimeProvider(
            ReservationTestData.VietnamTime(2026, 7, 22, 7, 0));
        return new ReservationService(
            data.UnitOfWork,
            new NotificationService(data.UnitOfWork, clock),
            clock);
    }

    private static ReservationAvailabilityRequest Request(int hour, int minute, int guests)
        => new()
        {
            Date = BookingDate,
            Time = new TimeOnly(hour, minute),
            NumberOfGuests = guests
        };
}
