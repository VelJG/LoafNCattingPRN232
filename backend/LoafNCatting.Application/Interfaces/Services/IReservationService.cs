using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetMineAsync(
        int customerUserId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> GetMineByIdAsync(
        int customerUserId,
        int reservationId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CreateAsync(
        int customerUserId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CancelByCustomerAsync(
        int customerUserId,
        int reservationId,
        CancellationToken cancellationToken = default);

    Task<ReservationAvailabilityDto> GetAvailabilityAsync(
        ReservationAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationLifecycleResultDto> ProcessDueReservationsAsync(
        CancellationToken cancellationToken = default);
}
