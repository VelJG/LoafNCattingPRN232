using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;

namespace LoafNCatting.Services.Services;

public sealed class PayOsPaymentGateway : IPaymentGateway
{
    private readonly PayOsSettings _settings;

    public PayOsPaymentGateway(IOptions<PayOsSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<PaymentGatewayLink> CreatePaymentLinkAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var paymentData = new PaymentData(
            request.OrderCode,
            request.Amount,
            request.Description,
            request.Items
                .Select(item => new ItemData(
                    item.Name,
                    item.Quantity,
                    item.UnitPrice))
                .ToList(),
            request.CancelUrl,
            request.ReturnUrl);
        var result = await CreateClient().createPaymentLink(paymentData);
        cancellationToken.ThrowIfCancellationRequested();

        return new PaymentGatewayLink(
            result.orderCode,
            result.amount,
            result.checkoutUrl,
            result.qrCode,
            result.paymentLinkId);
    }

    public async Task<PaymentGatewayStatus> GetPaymentStatusAsync(
        long orderCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await CreateClient().getPaymentLinkInformation(orderCode);
        cancellationToken.ThrowIfCancellationRequested();
        return new PaymentGatewayStatus(result.status);
    }

    private PayOS CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ChecksumKey))
        {
            throw new InvalidOperationException(
                "PayOS credentials are not configured. Set PayOS:ClientId, PayOS:ApiKey, and PayOS:ChecksumKey using user secrets or environment variables.");
        }

        return new PayOS(
            _settings.ClientId,
            _settings.ApiKey,
            _settings.ChecksumKey);
    }
}
