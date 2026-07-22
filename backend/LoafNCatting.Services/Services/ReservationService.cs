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
    private const int MinimumCustomerCancellationMinutes = 120;
    private const int TableHoldMinutes = 30;
    private const int EndingSoonReminderMinutes = 10;
    private const int ReminderDetectionWindowMinutes = 1;
    private const int MaximumGuestNameLength = 255;
    private const int MaximumGuestPhoneNumberLength = 20;

    private static readonly TimeOnly FirstBookableTime = new(8, 30);
    private static readonly TimeOnly LastBookableTime = new(20, 30);
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private const string PendingReservationStatus = "Đang chờ";
    private const string ConfirmedReservationStatus = "Đã xác nhận";
    private const string CancelledReservationStatus = "Đã hủy";
    private const string CompletedReservationStatus = "Hoàn thành";
    private const string CheckedInReservationStatus = "Đã đến";
    private const string NoShowReservationStatus = "Không đến";
    private const string ExpiredReservationStatus = "Hết hạn";
    private const string CustomerRoleName = "Customer";
    private const string StaffRoleName = "Staff";
    private const string AdminRoleName = "Admin";

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
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public ReservationService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetMineAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var reservations = await _unitOfWork.Repository<Reservation>()
            .Entities
            .AsNoTracking()
            .Include(reservation => reservation.Status)
            .Include(reservation => reservation.Table)
            .Where(reservation => reservation.UserId == customerUserId)
            .OrderByDescending(reservation => reservation.Date)
            .ThenByDescending(reservation => reservation.Time)
            .ThenByDescending(reservation => reservation.ReservationId)
            .ToListAsync(cancellationToken);

        return reservations
            .Select(MapReservation)
            .ToList();
    }

    public async Task<ReservationDto> GetMineByIdAsync(
        int customerUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);
        ValidateReservationId(reservationId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var reservation = await _unitOfWork.Repository<Reservation>()
            .Entities
            .AsNoTracking()
            .Include(current => current.Status)
            .Include(current => current.Table)
            .SingleOrDefaultAsync(
                current =>
                    current.ReservationId == reservationId &&
                    current.UserId == customerUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Reservation was not found.");

        return MapReservation(reservation);
    }

    public async Task<ReservationDto> CreateAsync(
        int customerUserId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);

        ArgumentNullException.ThrowIfNull(request);
        var (date, time, numberOfGuests, startAt, endAt) = ValidateRequest(
            new ReservationAvailabilityRequest
            {
                Date = request.Date,
                Time = request.Time,
                NumberOfGuests = request.NumberOfGuests
            });
        var guestName = RequiredTrimmed(request.GuestName, nameof(request.GuestName));
        var guestPhoneNumber = RequiredTrimmed(
            request.GuestPhoneNumber,
            nameof(request.GuestPhoneNumber));
        var note = string.IsNullOrWhiteSpace(request.Note)
            ? null
            : request.Note.Trim();

        if (guestName.Length > MaximumGuestNameLength)
        {
            throw new ArgumentException(
                $"Guest name cannot exceed {MaximumGuestNameLength} characters.",
                nameof(request));
        }

        if (guestPhoneNumber.Length > MaximumGuestPhoneNumberLength)
        {
            throw new ArgumentException(
                $"Guest phone number cannot exceed {MaximumGuestPhoneNumberLength} characters.",
                nameof(request));
        }

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

            var endTime = time.AddMinutes(ReservationDurationMinutes);
            var earliestOverlappingStart = time.AddMinutes(-ReservationDurationMinutes);
            var hasCustomerOverlap = await _unitOfWork.Repository<Reservation>()
                .Entities
                .AsNoTracking()
                .AnyAsync(
                    reservation =>
                        reservation.UserId == customerUserId &&
                        reservation.Date == date &&
                        BlockingReservationStatuses.Contains(reservation.Status.StatusName) &&
                        reservation.Time < endTime &&
                        reservation.Time > earliestOverlappingStart,
                    cancellationToken);
            if (hasCustomerOverlap)
            {
                throw new InvalidOperationException(
                    "The customer already has an overlapping active reservation.");
            }

            var suggestedTable = await FindSuggestedTableAsync(
                date,
                time,
                numberOfGuests,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "No table is available for the requested party and time slot.");
            var pendingStatus = await _unitOfWork.Repository<ReservationStatus>()
                .Entities
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    status => status.StatusName == PendingReservationStatus,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Required {nameof(ReservationStatus)} '{PendingReservationStatus}' is not configured.");
            var createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var reservation = new Reservation
            {
                UserId = customerUserId,
                Date = date,
                Time = time,
                GuestName = guestName,
                GuestPhoneNumber = guestPhoneNumber,
                NumberOfGuests = numberOfGuests,
                Note = note,
                StatusId = pendingStatus.StatusId,
                TableId = suggestedTable.TableId,
                CreatedAt = createdAtUtc
            };

            await _unitOfWork.Repository<Reservation>().InsertAsync(
                reservation,
                saveChanges: false);
            await _notificationService.QueueForActiveStaffAsync(
                new NotificationDraft(
                    "New reservation request",
                    $"{guestName} requested a table for {numberOfGuests} guest(s) on {date:dd/MM/yyyy} at {time:HH:mm}.",
                    NotificationTypes.ReservationCreated),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new ReservationDto(
                reservation.ReservationId,
                customerUserId,
                date,
                time,
                numberOfGuests,
                guestName,
                guestPhoneNumber,
                note,
                pendingStatus.StatusName,
                ReservationDurationMinutes,
                startAt,
                endAt,
                suggestedTable,
                createdAtUtc);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ReservationDto> CancelByCustomerAsync(
        int customerUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateCustomerUserId(customerUserId);
        ValidateReservationId(reservationId);
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

            var reservation = await _unitOfWork.Repository<Reservation>()
                .Entities
                .Include(current => current.Status)
                .Include(current => current.Table)
                .SingleOrDefaultAsync(
                    current =>
                        current.ReservationId == reservationId &&
                        current.UserId == customerUserId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Reservation was not found.");

            var currentStatusName = reservation.Status.StatusName;
            if (!string.Equals(
                    currentStatusName,
                    PendingReservationStatus,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    currentStatusName,
                    ConfirmedReservationStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only pending or confirmed reservations can be cancelled by the customer.");
            }

            var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);
            if (startAt - now < TimeSpan.FromMinutes(MinimumCustomerCancellationMinutes))
            {
                throw new InvalidOperationException(
                    "A reservation must be cancelled at least 2 hours before its start time.");
            }

            var cancelledStatus = await _unitOfWork.Repository<ReservationStatus>()
                .Entities
                .SingleOrDefaultAsync(
                    status => status.StatusName == CancelledReservationStatus,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Required {nameof(ReservationStatus)} '{CancelledReservationStatus}' is not configured.");

            reservation.StatusId = cancelledStatus.StatusId;
            reservation.Status = cancelledStatus;
            reservation.UpdatedAt = now.UtcDateTime;

            await _notificationService.QueueForActiveStaffAsync(
                new NotificationDraft(
                    "Reservation cancelled",
                    $"{reservation.GuestName} cancelled the reservation for {reservation.NumberOfGuests} guest(s) on {reservation.Date:dd/MM/yyyy} at {reservation.Time:HH:mm}.",
                    NotificationTypes.ReservationCancelled),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return MapReservation(reservation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<StoreReservationDto>> GetForStoreAsync(
        int operatorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperatorUserId(operatorUserId);
        await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);

        var reservations = await _unitOfWork.Repository<Reservation>()
            .Entities
            .AsNoTracking()
            .Include(reservation => reservation.User)
            .Include(reservation => reservation.Status)
            .Include(reservation => reservation.Table)
            .ThenInclude(table => table.TableStatus)
            .OrderByDescending(reservation => reservation.Date)
            .ThenByDescending(reservation => reservation.Time)
            .ThenByDescending(reservation => reservation.ReservationId)
            .ToListAsync(cancellationToken);

        return reservations
            .Select(MapStoreReservation)
            .ToList();
    }

    public async Task<StoreReservationDto> GetForStoreByIdAsync(
        int operatorUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperatorUserId(operatorUserId);
        ValidateReservationId(reservationId);
        await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);

        var reservation = await _unitOfWork.Repository<Reservation>()
            .Entities
            .AsNoTracking()
            .Include(current => current.User)
            .Include(current => current.Status)
            .Include(current => current.Table)
            .ThenInclude(table => table.TableStatus)
            .SingleOrDefaultAsync(
                current => current.ReservationId == reservationId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Reservation was not found.");

        return MapStoreReservation(reservation);
    }

    public async Task<StoreReservationDto> ConfirmByStoreAsync(
        int operatorUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperatorUserId(operatorUserId);
        ValidateReservationId(reservationId);
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);
            var reservation = await GetStoreReservationForUpdateAsync(
                reservationId,
                cancellationToken);

            EnsureReservationStatus(
                reservation,
                PendingReservationStatus,
                "Only pending reservations can be confirmed.");

            var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);
            if (now >= startAt)
            {
                throw new InvalidOperationException(
                    "A pending reservation cannot be confirmed at or after its start time.");
            }

            if (string.Equals(
                reservation.Table.TableStatus.StatusName,
                MaintenanceTableStatus,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The assigned table is under maintenance and the reservation cannot be confirmed.");
            }

            var confirmedStatus = await GetReservationStatusAsync(
                ConfirmedReservationStatus,
                cancellationToken);
            reservation.StatusId = confirmedStatus.StatusId;
            reservation.Status = confirmedStatus;
            reservation.UpdatedAt = now.UtcDateTime;

            await QueueCustomerNotificationAsync(
                reservation,
                new NotificationDraft(
                    "Reservation confirmed",
                    $"Your reservation on {reservation.Date:dd/MM/yyyy} at {reservation.Time:HH:mm} has been confirmed.",
                    NotificationTypes.ReservationConfirmed),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return MapStoreReservation(reservation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<StoreReservationDto> CancelByStoreAsync(
        int operatorUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperatorUserId(operatorUserId);
        ValidateReservationId(reservationId);
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);
            var reservation = await GetStoreReservationForUpdateAsync(
                reservationId,
                cancellationToken);

            var currentStatusName = reservation.Status.StatusName;
            if (!string.Equals(
                    currentStatusName,
                    PendingReservationStatus,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    currentStatusName,
                    ConfirmedReservationStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only pending or confirmed reservations can be cancelled by the store.");
            }

            var cancelledStatus = await GetReservationStatusAsync(
                CancelledReservationStatus,
                cancellationToken);
            reservation.StatusId = cancelledStatus.StatusId;
            reservation.Status = cancelledStatus;
            reservation.UpdatedAt = now.UtcDateTime;

            var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);
            if (now >= startAt && string.Equals(
                reservation.Table.TableStatus.StatusName,
                ReservedTableStatus,
                StringComparison.OrdinalIgnoreCase))
            {
                var availableStatus = await GetTableStatusAsync(
                    AvailableTableStatus,
                    cancellationToken);
                reservation.Table.TableStatusId = availableStatus.TableStatusId;
                reservation.Table.TableStatus = availableStatus;
            }

            await QueueCustomerNotificationAsync(
                reservation,
                new NotificationDraft(
                    "Reservation cancelled by store",
                    $"Your reservation on {reservation.Date:dd/MM/yyyy} at {reservation.Time:HH:mm} has been cancelled by the store.",
                    NotificationTypes.ReservationCancelled),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return MapStoreReservation(reservation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<StoreReservationDto> CheckInByStoreAsync(
        int operatorUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperatorUserId(operatorUserId);
        ValidateReservationId(reservationId);
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);
            var reservation = await GetStoreReservationForUpdateAsync(
                reservationId,
                cancellationToken);

            EnsureReservationStatus(
                reservation,
                ConfirmedReservationStatus,
                "Only confirmed reservations can be checked in.");

            var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);
            if (now < startAt)
            {
                throw new InvalidOperationException(
                    "A reservation cannot be checked in before its start time.");
            }

            if (now >= startAt.AddMinutes(TableHoldMinutes))
            {
                throw new InvalidOperationException(
                    "The 30-minute table hold window has elapsed.");
            }

            var currentTableStatusName = reservation.Table.TableStatus.StatusName;
            if (!string.Equals(
                    currentTableStatusName,
                    AvailableTableStatus,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    currentTableStatusName,
                    ReservedTableStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The assigned table cannot be occupied because its current status is '{currentTableStatusName}'.");
            }

            var checkedInStatus = await GetReservationStatusAsync(
                CheckedInReservationStatus,
                cancellationToken);
            var occupiedStatus = await GetTableStatusAsync(
                OccupiedTableStatus,
                cancellationToken);
            reservation.StatusId = checkedInStatus.StatusId;
            reservation.Status = checkedInStatus;
            reservation.UpdatedAt = now.UtcDateTime;
            reservation.Table.TableStatusId = occupiedStatus.TableStatusId;
            reservation.Table.TableStatus = occupiedStatus;

            await QueueCustomerNotificationAsync(
                reservation,
                new NotificationDraft(
                    "Reservation checked in",
                    $"You have checked in for the reservation at {reservation.Time:HH:mm}.",
                    NotificationTypes.ReservationCheckedIn),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return MapStoreReservation(reservation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<StoreReservationDto> CompleteByStoreAsync(
        int operatorUserId,
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        ValidateOperatorUserId(operatorUserId);
        ValidateReservationId(reservationId);
        var now = _timeProvider.GetUtcNow().ToOffset(VietnamUtcOffset);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);
            var reservation = await GetStoreReservationForUpdateAsync(
                reservationId,
                cancellationToken);

            EnsureReservationStatus(
                reservation,
                CheckedInReservationStatus,
                "Only checked-in reservations can be completed.");

            var currentTableStatusName = reservation.Table.TableStatus.StatusName;
            if (!string.Equals(
                    currentTableStatusName,
                    OccupiedTableStatus,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    currentTableStatusName,
                    AvailableTableStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The assigned table cannot be released because its current status is '{currentTableStatusName}'.");
            }

            var completedStatus = await GetReservationStatusAsync(
                CompletedReservationStatus,
                cancellationToken);
            var availableStatus = await GetTableStatusAsync(
                AvailableTableStatus,
                cancellationToken);
            reservation.StatusId = completedStatus.StatusId;
            reservation.Status = completedStatus;
            reservation.UpdatedAt = now.UtcDateTime;
            reservation.Table.TableStatusId = availableStatus.TableStatusId;
            reservation.Table.TableStatus = availableStatus;

            await QueueCustomerNotificationAsync(
                reservation,
                new NotificationDraft(
                    "Reservation completed",
                    "Your reservation has been completed. Thank you for visiting Loaf N' Catting.",
                    NotificationTypes.ReservationCompleted),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return MapStoreReservation(reservation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ReservationAvailabilityDto> GetAvailabilityAsync(
        ReservationAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (date, time, numberOfGuests, startAt, endAt) = ValidateRequest(request);
        var suggestedTable = await FindSuggestedTableAsync(
            date,
            time,
            numberOfGuests,
            cancellationToken);

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
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (now >= startAt)
                    {
                        reservation.StatusId = expiredStatus.StatusId;
                        reservation.Status = expiredStatus;
                        reservation.UpdatedAt = now.UtcDateTime;
                        markedExpired++;
                    }

                    continue;
                }

                if (string.Equals(
                        currentStatusName,
                        ConfirmedReservationStatus,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (now >= holdUntil)
                    {
                        reservation.StatusId = noShowStatus.StatusId;
                        reservation.Status = noShowStatus;
                        reservation.UpdatedAt = now.UtcDateTime;
                        markedNoShow++;

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

    private async Task<SuggestedTableDto?> FindSuggestedTableAsync(
        DateOnly date,
        TimeOnly time,
        int numberOfGuests,
        CancellationToken cancellationToken)
    {
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

        return await _unitOfWork.Repository<RestaurantTable>()
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
    }

    private static string RequiredTrimmed(string? value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName)
            : value.Trim();

    private async Task EnsureActiveCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken)
    {
        var isActiveCustomer = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.UserId == customerUserId &&
                    user.IsActive &&
                    user.Role.RoleName == CustomerRoleName,
                cancellationToken);
        if (!isActiveCustomer)
        {
            throw new UnauthorizedAccessException(
                "The authenticated customer account is not active or valid.");
        }
    }

    private async Task EnsureActiveStoreOperatorAsync(
        int operatorUserId,
        CancellationToken cancellationToken)
    {
        var isActiveStoreOperator = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.UserId == operatorUserId &&
                    user.IsActive &&
                    (user.Role.RoleName == StaffRoleName ||
                     user.Role.RoleName == AdminRoleName),
                cancellationToken);
        if (!isActiveStoreOperator)
        {
            throw new UnauthorizedAccessException(
                "The authenticated store operator account is not active or valid.");
        }
    }

    private async Task<Reservation> GetStoreReservationForUpdateAsync(
        int reservationId,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<Reservation>()
            .Entities
            .Include(reservation => reservation.User)
            .Include(reservation => reservation.Status)
            .Include(reservation => reservation.Table)
            .ThenInclude(table => table.TableStatus)
            .SingleOrDefaultAsync(
                reservation => reservation.ReservationId == reservationId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Reservation was not found.");

    private async Task<ReservationStatus> GetReservationStatusAsync(
        string statusName,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<ReservationStatus>()
            .Entities
            .SingleOrDefaultAsync(
                status => status.StatusName == statusName,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Required {nameof(ReservationStatus)} '{statusName}' is not configured.");

    private async Task<TableStatus> GetTableStatusAsync(
        string statusName,
        CancellationToken cancellationToken)
        => await _unitOfWork.Repository<TableStatus>()
            .Entities
            .SingleOrDefaultAsync(
                status => status.StatusName == statusName,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Required {nameof(TableStatus)} '{statusName}' is not configured.");

    private Task QueueCustomerNotificationAsync(
        Reservation reservation,
        NotificationDraft draft,
        CancellationToken cancellationToken)
        => reservation.UserId.HasValue && reservation.User?.IsActive == true
            ? _notificationService.QueueForUserAsync(
                reservation.UserId.Value,
                draft,
                cancellationToken)
            : Task.CompletedTask;

    private static void EnsureReservationStatus(
        Reservation reservation,
        string requiredStatusName,
        string errorMessage)
    {
        if (!string.Equals(
            reservation.Status.StatusName,
            requiredStatusName,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static ReservationDto MapReservation(Reservation reservation)
    {
        var customerUserId = reservation.UserId
            ?? throw new InvalidOperationException(
                "A customer reservation must have an owner.");
        var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);

        return new ReservationDto(
            reservation.ReservationId,
            customerUserId,
            reservation.Date,
            reservation.Time,
            reservation.NumberOfGuests,
            reservation.GuestName,
            reservation.GuestPhoneNumber,
            reservation.Note,
            reservation.Status.StatusName,
            ReservationDurationMinutes,
            startAt,
            startAt.AddMinutes(ReservationDurationMinutes),
            new SuggestedTableDto(
                reservation.Table.TableId,
                reservation.Table.TableName,
                reservation.Table.Capacity,
                reservation.Table.Area,
                reservation.Table.Description),
            reservation.CreatedAt);
    }

    private static StoreReservationDto MapStoreReservation(Reservation reservation)
    {
        var startAt = ToVietnamDateTimeOffset(reservation.Date, reservation.Time);

        return new StoreReservationDto(
            reservation.ReservationId,
            reservation.UserId,
            reservation.User?.Name,
            reservation.User?.Email,
            reservation.Date,
            reservation.Time,
            reservation.NumberOfGuests,
            reservation.GuestName,
            reservation.GuestPhoneNumber,
            reservation.Note,
            reservation.Status.StatusName,
            ReservationDurationMinutes,
            startAt,
            startAt.AddMinutes(ReservationDurationMinutes),
            new SuggestedTableDto(
                reservation.Table.TableId,
                reservation.Table.TableName,
                reservation.Table.Capacity,
                reservation.Table.Area,
                reservation.Table.Description),
            reservation.Table.TableStatus.StatusName,
            reservation.CreatedAt,
            reservation.UpdatedAt);
    }

    private static void ValidateCustomerUserId(int customerUserId)
    {
        if (customerUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerUserId),
                "Customer user id must be greater than zero.");
        }
    }

    private static void ValidateReservationId(int reservationId)
    {
        if (reservationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reservationId),
                "Reservation id must be greater than zero.");
        }
    }

    private static void ValidateOperatorUserId(int operatorUserId)
    {
        if (operatorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operatorUserId),
                "Store operator user id must be greater than zero.");
        }
    }

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
