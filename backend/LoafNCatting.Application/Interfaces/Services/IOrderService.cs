using LoafNCatting.Application.DTOs.Orders;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        int? userId,
        int? statusId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetOrdersForCustomerAsync(
        int customerUserId,
        int? statusId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetOrderForCustomerAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CheckoutAsync(
        int customerUserId,
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateStatusAsync(
        int orderId,
        OrderStatusUpdateRequest request,
        CancellationToken cancellationToken = default);
}
