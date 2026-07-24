using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.DTOs.Orders;

public sealed record CheckoutRequest(
    [param: Required, StringLength(50)] string OrderType,
    [param: Range(1, int.MaxValue)] int? TableId,
    [param: Range(1, int.MaxValue)] int? ReservationId,
    [param: Range(1, int.MaxValue)] int PaymentMethodId,
    string? Note);

public sealed record CheckoutOptionsDto(
    IReadOnlyList<string> OrderTypes,
    IReadOnlyList<PaymentMethodOptionDto> PaymentMethods);

public sealed record PaymentMethodOptionDto(
    int PaymentMethodId,
    string Name,
    string? Description);

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
    IReadOnlyList<PaymentDto> Payments,
    int? TableId,
    string? TableName,
    int? ReservationId);

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
    [param: Range(1, int.MaxValue)] int OrderStatusId);
