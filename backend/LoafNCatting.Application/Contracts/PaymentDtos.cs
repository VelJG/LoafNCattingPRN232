using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed record CreatePaymentLinkRequest(
    [param: Range(1, int.MaxValue)] int OrderId);

public sealed record PaymentLinkDto(
    int OrderId,
    long OrderCode,
    int Amount,
    string CheckoutUrl,
    string QrCode,
    string PaymentLinkId);

public sealed record PaymentStatusDto(
    int OrderId,
    string PaymentStatus,
    string OrderStatus,
    bool IsPaid);

public sealed record PaymentGatewayItem(
    string Name,
    int Quantity,
    int UnitPrice);

public sealed record PaymentGatewayRequest(
    long OrderCode,
    int Amount,
    string Description,
    IReadOnlyList<PaymentGatewayItem> Items,
    string CancelUrl,
    string ReturnUrl);

public sealed record PaymentGatewayLink(
    long OrderCode,
    int Amount,
    string CheckoutUrl,
    string QrCode,
    string PaymentLinkId);

public sealed record PaymentGatewayStatus(string Status);

public sealed class PayOsSettings
{
    public const string SectionName = "PayOS";

    public string ClientId { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string ChecksumKey { get; init; } = string.Empty;

    public string ReturnUrl { get; init; } =
        "http://localhost:5173/orders?payment=success";

    public string CancelUrl { get; init; } =
        "http://localhost:5173/orders?payment=cancelled";
}

public sealed class PaymentSettings
{
    public const string SectionName = "Payments";

    public int? PendingPaymentExpirySeconds { get; init; }

    public int PendingPaymentExpiryMinutes { get; init; } = 15;
}
