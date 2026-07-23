using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.DTOs.Orders;

public sealed record CheckoutRequest(
    [property: Range(1, int.MaxValue)] int UserId,
    [property: Required, StringLength(50)] string OrderType,
    [property: Range(1, int.MaxValue)] int? TableId,
    [property: Range(1, int.MaxValue)] int? ReservationId,
    [property: Range(1, int.MaxValue)] int PaymentMethodId,
    string? Note);

public sealed record OrderDto(
    int OrderId,
    int? CustomerUserId,
    string? CustomerName,
    DateTime OrderDate,
    decimal TotalPrice,
    string? OrderType,
    string? Note,
    int OrderStatusId,
    string OrderStatusName,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentDto> Payments);

public sealed record OrderItemDto(
    int OrderDetailId,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record PaymentDto(
    int PaymentId,
    decimal PaymentAmount,
    int MethodId,
    string MethodName,
    string PaymentStatus,
    string? TransactionCode,
    DateTime PaymentDate,
    DateTime? PaidAt);

public sealed record OrderStatusUpdateRequest(
    [property: Range(1, int.MaxValue)] int OrderStatusId);
