using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IPaymentGateway
{
    Task<PaymentGatewayLink> CreatePaymentLinkAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayStatus> GetPaymentStatusAsync(
        long orderCode,
        CancellationToken cancellationToken = default);
}
