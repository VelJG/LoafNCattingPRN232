using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed class ReservationAvailabilityRequest
{
    public DateOnly? Date { get; init; }

    public TimeOnly? Time { get; init; }

    public int? NumberOfGuests { get; init; }
}

public sealed record SuggestedTableDto(
    int TableId,
    string TableName,
    int Capacity,
    string? Area,
    string? Description);

public sealed record ReservationAvailabilityDto(
    bool IsAvailable,
    string? Reason,
    int DurationMinutes,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    SuggestedTableDto? SuggestedTable);

public sealed class CreateReservationRequest
{
    public DateOnly? Date { get; init; }

    public TimeOnly? Time { get; init; }

    [Range(1, int.MaxValue)]
    public int? NumberOfGuests { get; init; }

    [Required, MaxLength(255)]
    public string? GuestName { get; init; }

    [Required, MaxLength(20)]
    public string? GuestPhoneNumber { get; init; }

    public string? Note { get; init; }
}

public sealed record ReservationDto(
    int ReservationId,
    int CustomerUserId,
    DateOnly Date,
    TimeOnly Time,
    int NumberOfGuests,
    string GuestName,
    string GuestPhoneNumber,
    string? Note,
    string Status,
    int DurationMinutes,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    SuggestedTableDto Table,
    DateTime CreatedAtUtc);

public sealed record ReservationLifecycleConflictDto(
    int ReservationId,
    int TableId,
    string Reason);

public sealed record ReservationLifecycleResultDto(
    int TablesReserved,
    int TablesReleased,
    int ReservationsMarkedNoShow,
    int ReservationsMarkedExpired,
    IReadOnlyList<int> EndingSoonReservationIds,
    IReadOnlyList<ReservationLifecycleConflictDto> Conflicts);
