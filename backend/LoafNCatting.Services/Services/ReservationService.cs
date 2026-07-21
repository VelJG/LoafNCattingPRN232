using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace LoafNCatting.Services.Services;

public sealed class ReservationService : IReservationService
{
    private const int ReservationDurationMinutes = 90;
    private const int MinimumAdvanceMinutes = 30;
    private const int MaximumAdvanceDays = 7;
    private const int TableHoldMinutes = 30;
    private const int EndingSoonReminderMinutes = 10;
    private const int ReminderDetectionWindowMinutes = 1;

    private static readonly TimeOnly FirstBookableTime = new(8, 30);
    private static readonly TimeOnly LastBookableTime = new(20, 30);
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private const string PendingReservationStatus = "Đang chờ";
    private const string ConfirmedReservationStatus = "Đã xác nhận";
    private const string CheckedInReservationStatus = "Đã đến";
    private const string NoShowReservationStatus = "Không đến";
    private const string ExpiredReservationStatus = "Hết hạn";

    private static readonly string[] BlockingReservationStatuses =
    [
        PendingReservationStatus,
        ConfirmedReservationStatus,
        CheckedInReservationStatus
    ];

    private const string AvailableTableStatus = "Trống";
    private const string ReservedTableStatus = "Đã đặt";
    private const string OccupiedTableStatus = "Đang sử dụng";
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

    public async Task<ReservationLifecycleResultDto> ProcessDueReservationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);
        var today = DateOnly.FromDateTime(now.DateTime);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var reservationStatuses = (await _unitOfWork.Repository<ReservationStatus>()
                    .Entities
                    .Where(status =>
                        BlockingReservationStatuses.Contains(status.StatusName) ||
                        status.StatusName == NoShowReservationStatus ||
                        status.StatusName == ExpiredReservationStatus)
                    .ToListAsync(cancellationToken))
                .ToDictionary(status => status.StatusName, StringComparer.OrdinalIgnoreCase);

            var tableStatuses = (await _unitOfWork.Repository<TableStatus>()
                    .Entities
                    .Where(status =>
                        status.StatusName == AvailableTableStatus ||
                        status.StatusName == ReservedTableStatus ||
                        status.StatusName == OccupiedTableStatus ||
                        status.StatusName == MaintenanceTableStatus)
                    .ToListAsync(cancellationToken))
                .ToDictionary(status => status.StatusName, StringComparer.OrdinalIgnoreCase);

            var noShowStatus = GetRequiredStatus(
                reservationStatuses,
                NoShowReservationStatus,
                nameof(ReservationStatus));
            var expiredStatus = GetRequiredStatus(
                reservationStatuses,
                ExpiredReservationStatus,
                nameof(ReservationStatus));
            var availableStatus = GetRequiredStatus(
                tableStatuses,
                AvailableTableStatus,
                nameof(TableStatus));
            var reservedStatus = GetRequiredStatus(
                tableStatuses,
                ReservedTableStatus,
                nameof(TableStatus));

            var candidates = await _unitOfWork.Repository<Reservation>()
                .Entities
                .Include(reservation => reservation.Status)
                .Include(reservation => reservation.Table)
                .ThenInclude(table => table.TableStatus)
                .Where(reservation =>
                    reservation.Date <= today &&
                    BlockingReservationStatuses.Contains(reservation.Status.StatusName))
                .OrderBy(reservation => reservation.Date)
                .ThenBy(reservation => reservation.Time)
                .ThenBy(reservation => reservation.ReservationId)
                .ToListAsync(cancellationToken);

            var tablesReserved = 0;
            var tablesReleased = 0;
            var markedNoShow = 0;
            var markedExpired = 0;
            var endingSoonReservationIds = new List<int>();
            var conflicts = new List<ReservationLifecycleConflictDto>();

            foreach (var reservation in candidates)
            {
                var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);
                var holdUntil = startAt.AddMinutes(TableHoldMinutes);
                var endAt = startAt.AddMinutes(ReservationDurationMinutes);
                var currentStatusName = reservation.Status.StatusName;

                if (string.Equals(
                        currentStatusName,
                        PendingReservationStatus,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        currentStatusName,
                        ConfirmedReservationStatus,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (now >= holdUntil)
                    {
                        var targetStatus = string.Equals(
                            currentStatusName,
                            ConfirmedReservationStatus,
                            StringComparison.OrdinalIgnoreCase)
                            ? noShowStatus
                            : expiredStatus;

                        reservation.StatusId = targetStatus.StatusId;
                        reservation.Status = targetStatus;
                        reservation.UpdatedAt = now.UtcDateTime;

                        if (targetStatus.StatusId == noShowStatus.StatusId)
                        {
                            markedNoShow++;
                        }
                        else
                        {
                            markedExpired++;
                        }

                        if (string.Equals(
                            reservation.Table.TableStatus.StatusName,
                            ReservedTableStatus,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            reservation.Table.TableStatusId = availableStatus.TableStatusId;
                            reservation.Table.TableStatus = availableStatus;
                            tablesReleased++;
                        }
                        else if (!string.Equals(
                            reservation.Table.TableStatus.StatusName,
                            AvailableTableStatus,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            conflicts.Add(new ReservationLifecycleConflictDto(
                                reservation.ReservationId,
                                reservation.TableId,
                                $"Table was not released because its current status is '{reservation.Table.TableStatus.StatusName}'."));
                        }

                        continue;
                    }

                    if (now >= startAt)
                    {
                        if (string.Equals(
                            reservation.Table.TableStatus.StatusName,
                            AvailableTableStatus,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            reservation.Table.TableStatusId = reservedStatus.TableStatusId;
                            reservation.Table.TableStatus = reservedStatus;
                            tablesReserved++;
                        }
                        else if (!string.Equals(
                            reservation.Table.TableStatus.StatusName,
                            ReservedTableStatus,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            conflicts.Add(new ReservationLifecycleConflictDto(
                                reservation.ReservationId,
                                reservation.TableId,
                                $"Table was not reserved because its current status is '{reservation.Table.TableStatus.StatusName}'."));
                        }
                    }

                    continue;
                }

                if (string.Equals(
                        currentStatusName,
                        CheckedInReservationStatus,
                        StringComparison.OrdinalIgnoreCase) &&
                    now >= endAt.AddMinutes(-EndingSoonReminderMinutes) &&
                    now < endAt.AddMinutes(
                        -EndingSoonReminderMinutes + ReminderDetectionWindowMinutes))
                {
                    endingSoonReservationIds.Add(reservation.ReservationId);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ReservationLifecycleResultDto(
                tablesReserved,
                tablesReleased,
                markedNoShow,
                markedExpired,
                endingSoonReservationIds,
                conflicts);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
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

    private static DateTimeOffset ToVietnamDateTimeOffset(DateOnly date, TimeOnly time)
        => new(
            date.Year,
            date.Month,
            date.Day,
            time.Hour,
            time.Minute,
            time.Second,
            VietnamUtcOffset);

    private static TStatus GetRequiredStatus<TStatus>(
        IReadOnlyDictionary<string, TStatus> statuses,
        string statusName,
        string entityName)
        where TStatus : class
        => statuses.TryGetValue(statusName, out var status)
            ? status
            : throw new InvalidOperationException(
                $"Required {entityName} '{statusName}' is not configured.");
}
