using LoafNCatting.Application.DTOs.Orders;

namespace LoafNCatting.Application.Interfaces.Services;

public interface IOrderService
{
    Task<CheckoutOptionsDto> GetCheckoutOptionsAsync(
        int customerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetMineAsync(
        int customerUserId,
        int? statusId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetMineByIdAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CheckoutAsync(
        int customerUserId,
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetForStoreAsync(
        int operatorUserId,
        int? statusId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetForStoreByIdAsync(
        int operatorUserId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateStatusByStoreAsync(
        int operatorUserId,
        int orderId,
        OrderStatusUpdateRequest request,
        CancellationToken cancellationToken = default);
}
