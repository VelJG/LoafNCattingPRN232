using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using LoafNCatting.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class ReservationWalkInServiceTests
{
    private static readonly DateOnly BusinessDate = new(2026, 7, 22);

    [TestMethod]
    public async Task CreateWalkInAsync_CreatesCheckedInGuestReservationAndOccupiesSmallestTable()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 4);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, 17, 10).CreateWalkInAsync(
            20,
            WalkInRequest(numberOfGuests: 2, phoneNumber: null));

        var stored = await data.DbContext.Reservations.AsNoTracking().SingleAsync();
        var selectedTable = await data.DbContext.RestaurantTables
            .AsNoTracking()
            .SingleAsync(table => table.TableId == 1);
        Assert.IsNull(stored.UserId);
        Assert.AreEqual(BusinessDate, stored.Date);
        Assert.AreEqual(new TimeOnly(17, 10), stored.Time);
        Assert.AreEqual(string.Empty, stored.GuestPhoneNumber);
        Assert.AreEqual(ReservationTestData.CheckedInStatusId, stored.StatusId);
        Assert.AreEqual(1, stored.TableId);
        Assert.AreEqual(ReservationTestData.OccupiedTableStatusId, selectedTable.TableStatusId);
        Assert.IsNull(result.CustomerUserId);
        Assert.AreEqual("Đã đến", result.Status);
        Assert.AreEqual("Đang sử dụng", result.TableStatus);
        Assert.AreEqual(1, result.Table.TableId);
        Assert.AreEqual(new TimeOnly(17, 10), result.Time);
        Assert.AreEqual(new DateTimeOffset(2026, 7, 22, 19, 10, 0, TimeSpan.FromHours(7)), result.EndAt);
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    [TestMethod]
    public async Task CreateWalkInAsync_ExcludesTableWhoseFutureReservationOverlapsVisit()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddTable(data, tableId: 2, capacity: 4);
        ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            BusinessDate,
            new TimeOnly(18, 0),
            ReservationTestData.ConfirmedStatusId);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, 17, 10).CreateWalkInAsync(
            20,
            WalkInRequest(numberOfGuests: 2));

        Assert.AreEqual(2, result.Table.TableId);
        Assert.AreEqual(
            ReservationTestData.AvailableTableStatusId,
            await data.DbContext.RestaurantTables
                .Where(table => table.TableId == 1)
                .Select(table => table.TableStatusId)
                .SingleAsync());
    }

    [TestMethod]
    public async Task CreateWalkInAsync_AllowsTableWhenNextReservationStartsAtExactEnd()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        ReservationTestData.AddReservation(
            data,
            reservationId: 1,
            tableId: 1,
            BusinessDate,
            new TimeOnly(19, 10),
            ReservationTestData.PendingStatusId);
        await data.DbContext.SaveChangesAsync();

        var result = await CreateService(data, 17, 10).CreateWalkInAsync(
            20,
            WalkInRequest(numberOfGuests: 2));

        Assert.AreEqual(1, result.Table.TableId);
    }

    [TestMethod]
    public async Task CreateWalkInAsync_DoesNotOverwriteOccupiedTable()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(
            data,
            tableId: 1,
            capacity: 4,
            tableStatusId: ReservationTestData.OccupiedTableStatusId);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, 17, 10).CreateWalkInAsync(
                20,
                WalkInRequest(numberOfGuests: 2)));

        Assert.AreEqual(0, await data.DbContext.Reservations.CountAsync());
        Assert.AreEqual(
            ReservationTestData.OccupiedTableStatusId,
            await data.DbContext.RestaurantTables
                .Select(table => table.TableStatusId)
                .SingleAsync());
    }

    [TestMethod]
    [DataRow(7, 59)]
    [DataRow(20, 1)]
    public async Task CreateWalkInAsync_OutsideOperatingWindow_IsRejected(
        int hour,
        int minute)
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            CreateService(data, hour, minute).CreateWalkInAsync(
                20,
                WalkInRequest(numberOfGuests: 2)));

        Assert.AreEqual(0, await data.DbContext.Reservations.CountAsync());
    }

    [TestMethod]
    public async Task CreateWalkInAsync_WithInactiveStaff_IsRejected()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            CreateService(data, 17, 10).CreateWalkInAsync(
                21,
                WalkInRequest(numberOfGuests: 2)));

        Assert.AreEqual(0, await data.DbContext.Reservations.CountAsync());
    }

    [TestMethod]
    public async Task CompleteByStoreAsync_ForWalkIn_ReleasesTableWithoutCustomerNotification()
    {
        await using var data = await CreateDataAsync();
        ReservationTestData.AddTable(data, tableId: 1, capacity: 2);
        await data.DbContext.SaveChangesAsync();
        var created = await CreateService(data, 17, 10).CreateWalkInAsync(
            20,
            WalkInRequest(numberOfGuests: 2));

        var completed = await CreateService(data, 17, 20).CompleteByStoreAsync(
            20,
            created.ReservationId);

        Assert.AreEqual("Hoàn thành", completed.Status);
        Assert.AreEqual("Trống", completed.TableStatus);
        Assert.AreEqual(
            ReservationTestData.AvailableTableStatusId,
            await data.DbContext.RestaurantTables
                .Select(table => table.TableStatusId)
                .SingleAsync());
        Assert.AreEqual(0, await data.DbContext.Notifications.CountAsync());
    }

    private static async Task<TestDataContext> CreateDataAsync()
    {
        var data = new TestDataContext();
        await data.SeedRolesAsync();
        await ReservationTestData.SeedStatusesAsync(data);
        AddUser(data, userId: 20, roleId: 2, isActive: true);
        AddUser(data, userId: 21, roleId: 2, isActive: false);
        AddUser(data, userId: 30, roleId: 1, isActive: true);
        await data.DbContext.SaveChangesAsync();
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

    private static CreateWalkInRequest WalkInRequest(
        int numberOfGuests,
        string? phoneNumber = "0987654321")
        => new()
        {
            NumberOfGuests = numberOfGuests,
            GuestName = "  Walk-in Guest  ",
            GuestPhoneNumber = phoneNumber,
            Note = "  Near the window  "
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
}
