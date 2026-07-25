using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;

namespace LoafNCatting.Tests;

internal sealed class TestPaymentGateway : IPaymentGateway
{
    public PaymentGatewayRequest? LastRequest { get; private set; }

    public string Status { get; set; } = "PENDING";

    public Task<PaymentGatewayLink> CreatePaymentLinkAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;
        return Task.FromResult(new PaymentGatewayLink(
            request.OrderCode,
            request.Amount,
            $"https://pay.test/{request.OrderCode}",
            $"qr-{request.OrderCode}",
            $"link-{request.OrderCode}"));
    }

    public Task<PaymentGatewayStatus> GetPaymentStatusAsync(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentGatewayStatus(Status));
    }
}
