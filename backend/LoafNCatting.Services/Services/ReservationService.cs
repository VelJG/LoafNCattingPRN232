using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class ReservationService : IReservationService
{
    private const int ReservationDurationMinutes = 90;
    private const int MinimumAdvanceMinutes = 30;
    private const int MaximumAdvanceDays = 7;

    private static readonly TimeOnly FirstBookableTime = new(8, 30);
    private static readonly TimeOnly LastBookableTime = new(20, 30);
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private static readonly string[] BlockingReservationStatuses =
    [
        "Đang chờ",
        "Đã xác nhận",
        "Đã đến"
    ];

    private const string MaintenanceTableStatus = "Bảo trì";

    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ReservationService(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ReservationAvailabilityDto> GetAvailabilityAsync(
        ReservationAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (date, time, numberOfGuests, startAt, endAt) = ValidateRequest(request);
        var endTime = time.AddMinutes(ReservationDurationMinutes);
        var earliestOverlappingStart = time.AddMinutes(-ReservationDurationMinutes);

        var conflictingTableIds = _unitOfWork.Repository<Reservation>()
            .Entities
            .AsNoTracking()
            .Where(reservation =>
                reservation.Date == date &&
                BlockingReservationStatuses.Contains(reservation.Status.StatusName) &&
                reservation.Time < endTime &&
                reservation.Time > earliestOverlappingStart)
            .Select(reservation => reservation.TableId)
            .Distinct();

        var suggestedTable = await _unitOfWork.Repository<RestaurantTable>()
            .Entities
            .AsNoTracking()
            .Where(table =>
                table.Capacity >= numberOfGuests &&
                table.TableStatus.StatusName != MaintenanceTableStatus &&
                !conflictingTableIds.Contains(table.TableId))
            .OrderBy(table => table.Capacity)
            .ThenBy(table => table.TableId)
            .Select(table => new SuggestedTableDto(
                table.TableId,
                table.TableName,
                table.Capacity,
                table.Area,
                table.Description))
            .FirstOrDefaultAsync(cancellationToken);

        return suggestedTable is null
            ? new ReservationAvailabilityDto(
                false,
                "No table is available for the requested party and time slot.",
                ReservationDurationMinutes,
                startAt,
                endAt,
                null)
            : new ReservationAvailabilityDto(
                true,
                null,
                ReservationDurationMinutes,
                startAt,
                endAt,
                suggestedTable);
    }

    private (DateOnly Date, TimeOnly Time, int NumberOfGuests, DateTimeOffset StartAt, DateTimeOffset EndAt)
        ValidateRequest(ReservationAvailabilityRequest request)
    {
        var date = request.Date
            ?? throw new ArgumentException("Reservation date is required.");
        var time = request.Time
            ?? throw new ArgumentException("Reservation time is required.");
        var numberOfGuests = request.NumberOfGuests
            ?? throw new ArgumentException("Number of guests is required.");

        if (numberOfGuests <= 0)
        {
            throw new ArgumentException("Number of guests must be greater than zero.");
        }

        if (time < FirstBookableTime || time > LastBookableTime)
        {
            throw new ArgumentException(
                "Reservation time must be between 08:30 and 20:30.");
        }

        if (time.Ticks % TimeSpan.TicksPerMinute != 0 || time.Minute % 30 != 0)
        {
            throw new ArgumentException(
                "Reservation time must use a 30-minute slot, for example 08:30, 09:00, or 09:30.");
        }

        var startAt = new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            time.Hour,
            time.Minute,
            0,
            VietnamUtcOffset);
        var endAt = startAt.AddMinutes(ReservationDurationMinutes);
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);

        if (startAt < now.AddMinutes(MinimumAdvanceMinutes))
        {
            throw new ArgumentException(
                "Reservations must be made at least 30 minutes in advance.");
        }

        if (startAt > now.AddDays(MaximumAdvanceDays))
        {
            throw new ArgumentException(
                "Reservations can be made at most 7 days in advance.");
        }

        return (date, time, numberOfGuests, startAt, endAt);
    }
}
