using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IReservationService
{
    Task<ReservationAvailabilityDto> GetAvailabilityAsync(
        ReservationAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationLifecycleResultDto> ProcessDueReservationsAsync(
        CancellationToken cancellationToken = default);
}
