using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(
        int customerUserId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationAvailabilityDto> GetAvailabilityAsync(
        ReservationAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationLifecycleResultDto> ProcessDueReservationsAsync(
        CancellationToken cancellationToken = default);
}
