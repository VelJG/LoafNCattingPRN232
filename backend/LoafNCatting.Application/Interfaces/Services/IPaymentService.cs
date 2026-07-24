using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<PaymentLinkDto> CreatePaymentLinkAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<PaymentStatusDto> GetPaymentStatusAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default);
}
