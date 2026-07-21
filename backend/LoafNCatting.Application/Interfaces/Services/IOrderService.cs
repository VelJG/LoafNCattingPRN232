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

    Task<OrderDto> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateStatusAsync(
        int orderId,
        OrderStatusUpdateRequest request,
        CancellationToken cancellationToken = default);
}
