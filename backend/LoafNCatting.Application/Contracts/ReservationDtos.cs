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
