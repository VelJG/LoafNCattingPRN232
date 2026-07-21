using LoafNCatting.Application.Interfaces.Services;

namespace LoafNCatting.WebApi.BackgroundServices;

public sealed class ReservationLifecycleBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationLifecycleBackgroundService> _logger;

    public ReservationLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    private async Task ProcessOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var reservationService = scope.ServiceProvider
                .GetRequiredService<IReservationService>();
            var result = await reservationService.ProcessDueReservationsAsync(stoppingToken);

            if (result.TablesReserved > 0 ||
                result.TablesReleased > 0 ||
                result.ReservationsMarkedNoShow > 0 ||
                result.ReservationsMarkedExpired > 0)
            {
                _logger.LogInformation(
                    "Reservation lifecycle processed: {TablesReserved} table(s) reserved, {TablesReleased} table(s) released, {NoShow} reservation(s) marked no-show, {Expired} reservation(s) expired.",
                    result.TablesReserved,
                    result.TablesReleased,
                    result.ReservationsMarkedNoShow,
                    result.ReservationsMarkedExpired);
            }

            foreach (var reservationId in result.EndingSoonReservationIds)
            {
                _logger.LogInformation(
                    "Reservation {ReservationId} will reach its 90-minute end time in 10 minutes. Store notification will be added after Auth integration.",
                    reservationId);
            }

            foreach (var conflict in result.Conflicts)
            {
                _logger.LogWarning(
                    "Reservation lifecycle conflict for reservation {ReservationId}, table {TableId}: {Reason}",
                    conflict.ReservationId,
                    conflict.TableId,
                    conflict.Reason);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Reservation lifecycle processing failed. The worker will retry on the next interval.");
        }
    }
}
