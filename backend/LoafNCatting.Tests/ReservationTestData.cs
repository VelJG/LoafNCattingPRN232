using LoafNCatting.Entity.Models;

namespace LoafNCatting.Tests;

internal static class ReservationTestData
{
    internal const int PendingStatusId = 1;
    internal const int ConfirmedStatusId = 2;
    internal const int CancelledStatusId = 3;
    internal const int CompletedStatusId = 4;
    internal const int NoShowStatusId = 5;
    internal const int CheckedInStatusId = 6;
    internal const int ExpiredStatusId = 7;

    internal const int AvailableTableStatusId = 1;
    internal const int ReservedTableStatusId = 2;
    internal const int OccupiedTableStatusId = 3;
    internal const int MaintenanceTableStatusId = 4;

    internal static async Task SeedStatusesAsync(TestDataContext data)
    {
        data.DbContext.ReservationStatuses.AddRange(
            new ReservationStatus { StatusId = PendingStatusId, StatusName = "Đang chờ" },
            new ReservationStatus { StatusId = ConfirmedStatusId, StatusName = "Đã xác nhận" },
            new ReservationStatus { StatusId = CancelledStatusId, StatusName = "Đã hủy" },
            new ReservationStatus { StatusId = CompletedStatusId, StatusName = "Hoàn thành" },
            new ReservationStatus { StatusId = NoShowStatusId, StatusName = "Không đến" },
            new ReservationStatus { StatusId = CheckedInStatusId, StatusName = "Đã đến" },
            new ReservationStatus { StatusId = ExpiredStatusId, StatusName = "Hết hạn" });

        data.DbContext.TableStatuses.AddRange(
            new TableStatus { TableStatusId = AvailableTableStatusId, StatusName = "Trống" },
            new TableStatus { TableStatusId = ReservedTableStatusId, StatusName = "Đã đặt" },
            new TableStatus { TableStatusId = OccupiedTableStatusId, StatusName = "Đang sử dụng" },
            new TableStatus { TableStatusId = MaintenanceTableStatusId, StatusName = "Bảo trì" });

        await data.DbContext.SaveChangesAsync();
    }

    internal static RestaurantTable AddTable(
        TestDataContext data,
        int tableId,
        int capacity,
        int tableStatusId = AvailableTableStatusId,
        string? tableName = null)
    {
        var table = new RestaurantTable
        {
            TableId = tableId,
            TableName = tableName ?? $"T{tableId}",
            Capacity = capacity,
            Area = "Test area",
            TableStatusId = tableStatusId
        };
        data.DbContext.RestaurantTables.Add(table);
        return table;
    }

    internal static Reservation AddReservation(
        TestDataContext data,
        int reservationId,
        int tableId,
        DateOnly date,
        TimeOnly time,
        int statusId)
    {
        var reservation = new Reservation
        {
            ReservationId = reservationId,
            Date = date,
            Time = time,
            GuestName = $"Guest {reservationId}",
            GuestPhoneNumber = $"090000{reservationId:D4}",
            NumberOfGuests = 2,
            StatusId = statusId,
            TableId = tableId,
            CreatedAt = DateTime.UtcNow
        };
        data.DbContext.Reservations.Add(reservation);
        return reservation;
    }

    internal static DateTimeOffset VietnamTime(
        int year,
        int month,
        int day,
        int hour,
        int minute = 0)
        => new(year, month, day, hour, minute, 0, TimeSpan.FromHours(7));
}

internal sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow.ToUniversalTime();

    public override DateTimeOffset GetUtcNow() => _utcNow;

    internal void SetUtcNow(DateTimeOffset value)
        => _utcNow = value.ToUniversalTime();
}
