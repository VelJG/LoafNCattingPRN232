using System.Data;
using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        int? userId,
        int? statusId,
        CancellationToken cancellationToken = default)
    {
        if (userId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User id must be greater than zero.");
        }

        if (statusId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusId), "Status id must be greater than zero.");
        }

        var query = OrderQuery(trackChanges: false);
        if (userId.HasValue)
        {
            query = query.Where(order => order.CustomerUserId == userId.Value);
        }

        if (statusId.HasValue)
        {
            query = query.Where(order => order.OrderStatusId == statusId.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync(cancellationToken);

        return orders.Select(ToOrderDto).ToList();
    }

    public async Task<OrderDto> GetOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateOrderId(orderId);
        var order = await OrderQuery(trackChanges: false)
            .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        return ToOrderDto(order);
    }

    public async Task<OrderDto> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCheckout(request);
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        int orderId;
        try
        {
            await EnsureUserExistsAsync(request.UserId, cancellationToken);
            var cart = await FindCartAsync(request.UserId, cancellationToken);
            if (cart is null || cart.CartItems.Count == 0)
            {
                throw new InvalidOperationException("Cart is empty.");
            }

            var paymentMethod = await Set<PaymentMethod>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    method => method.MethodId == request.PaymentMethodId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Payment method not found.");

            var reservation = await GetValidatedReservationAsync(request, cancellationToken);
            var tableId = request.TableId ?? reservation?.TableId;
            if (tableId.HasValue &&
                !await Set<RestaurantTable>()
                    .AsNoTracking()
                    .AnyAsync(table => table.TableId == tableId.Value, cancellationToken))
            {
                throw new KeyNotFoundException("Table not found.");
            }

            var requestedItems = cart.CartItems
                .GroupBy(item => item.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToList();

            if (requestedItems.Any(item => item.Quantity <= 0))
            {
                throw new InvalidOperationException("Cart contains an invalid quantity.");
            }

            var productIds = requestedItems.Select(item => item.ProductId).ToList();
            var products = await Set<Product>()
                .AsNoTracking()
                .Where(product => productIds.Contains(product.ProductId))
                .ToDictionaryAsync(product => product.ProductId, cancellationToken);

            if (products.Count != productIds.Count)
            {
                throw new KeyNotFoundException("One or more products no longer exist.");
            }

            foreach (var item in requestedItems)
            {
                var product = products[item.ProductId];
                if (!product.IsAvailable || product.UnitInStock < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product '{product.Name}'.");
                }

                var updatedRows = await Set<Product>()
                    .Where(current =>
                        current.ProductId == item.ProductId &&
                        current.IsAvailable &&
                        current.UnitInStock >= item.Quantity)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                current => current.UnitInStock,
                                current => current.UnitInStock - item.Quantity)
                            .SetProperty(
                                current => current.IsAvailable,
                                current => current.UnitInStock - item.Quantity > 0)
                            .SetProperty(current => current.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                if (updatedRows != 1)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product '{product.Name}'.");
                }
            }

            var pendingStatusId = await GetPendingOrderStatusIdAsync(cancellationToken);
            var subtotal = requestedItems.Sum(
                item => CurrentPrice(products[item.ProductId]) * item.Quantity);
            var orderType = request.OrderType.Trim();
            var serviceFee = string.Equals(
                orderType,
                "Takeaway",
                StringComparison.OrdinalIgnoreCase)
                ? 0m
                : Math.Round(subtotal * 0.05m, 0);
            var now = DateTime.UtcNow;

            var order = new Order
            {
                CustomerUserId = request.UserId,
                TableId = tableId,
                ReservationId = request.ReservationId,
                OrderType = orderType,
                Note = request.Note,
                OrderStatusId = pendingStatusId,
                TotalPrice = subtotal + serviceFee,
                OrderDate = now,
                CreatedAt = now
            };

            foreach (var item in requestedItems)
            {
                var product = products[item.ProductId];
                var unitPrice = CurrentPrice(product);
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = product.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = unitPrice * item.Quantity
                });
            }

            var requiresOnlinePayment = RequiresOnlinePayment(paymentMethod.MethodName);
            order.Payments.Add(new Payment
            {
                MethodId = paymentMethod.MethodId,
                PaymentAmount = order.TotalPrice,
                PaymentStatus = requiresOnlinePayment ? "Pending" : "Paid",
                TransactionCode = requiresOnlinePayment
                    ? null
                    : $"DEMO-{now:yyyyMMddHHmmssfff}",
                PaymentDate = now,
                PaidAt = requiresOnlinePayment ? null : now
            });

            Set<Order>().Add(order);
            Set<CartItem>().RemoveRange(cart.CartItems.ToList());
            cart.CartItems.Clear();
            cart.UpdatedAt = now;
            AddOrderNotification(
                request.UserId,
                "Order placed",
                "Your order has been created successfully.");

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            orderId = order.OrderId;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return await GetOrderAsync(orderId, cancellationToken);
    }

    public async Task<OrderDto> UpdateStatusAsync(
        int orderId,
        OrderStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateOrderId(orderId);
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var targetStatus = await Set<OrderStatus>()
                .FirstOrDefaultAsync(
                    status => status.OrderStatusId == request.OrderStatusId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Order status not found.");

            var order = await OrderQuery(trackChanges: true)
                .FirstOrDefaultAsync(current => current.OrderId == orderId, cancellationToken)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.OrderStatusId != targetStatus.OrderStatusId)
            {
                if (IsFinishedStatus(order.OrderStatus.OrderStatusName))
                {
                    throw new InvalidOperationException(
                        "A finished order cannot change status.");
                }

                if (IsCancelledStatus(targetStatus.OrderStatusName))
                {
                    RestoreStock(order);
                    foreach (var payment in order.Payments.Where(
                                 current => IsPendingPaymentStatus(current.PaymentStatus)))
                    {
                        payment.PaymentStatus = "Cancelled";
                    }
                }

                order.OrderStatusId = targetStatus.OrderStatusId;
                order.OrderStatus = targetStatus;
                order.UpdatedAt = DateTime.UtcNow;
                if (order.CustomerUserId.HasValue)
                {
                    AddOrderNotification(
                        order.CustomerUserId.Value,
                        "Order updated",
                        $"Order #{order.OrderId} is now {targetStatus.OrderStatusName}.");
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return await GetOrderAsync(orderId, cancellationToken);
    }

    private DbSet<T> Set<T>() where T : class
        => _unitOfWork.Repository<T>().Entities;

    private IQueryable<Order> OrderQuery(bool trackChanges)
    {
        IQueryable<Order> query = Set<Order>()
            .Include(order => order.CustomerUser)
            .Include(order => order.OrderStatus)
            .Include(order => order.OrderDetails)
            .ThenInclude(detail => detail.Product)
            .Include(order => order.Payments)
            .ThenInclude(payment => payment.Method);

        return trackChanges ? query : query.AsNoTracking();
    }

    private Task<Cart?> FindCartAsync(
        int userId,
        CancellationToken cancellationToken)
        => Set<Cart>()
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cart => cart.UserId == userId, cancellationToken);

    private async Task<Reservation?> GetValidatedReservationAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ReservationId.HasValue)
        {
            return null;
        }

        var reservation = await Set<Reservation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                current => current.ReservationId == request.ReservationId.Value,
                cancellationToken)
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.UserId.HasValue && reservation.UserId != request.UserId)
        {
            throw new InvalidOperationException(
                "Reservation does not belong to this user.");
        }

        if (request.TableId.HasValue && request.TableId.Value != reservation.TableId)
        {
            throw new InvalidOperationException(
                "Table does not match the reservation.");
        }

        return reservation;
    }

    private async Task EnsureUserExistsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (!await Set<User>()
                .AsNoTracking()
                .AnyAsync(user => user.UserId == userId, cancellationToken))
        {
            throw new KeyNotFoundException("User not found.");
        }
    }

    private async Task<int> GetPendingOrderStatusIdAsync(
        CancellationToken cancellationToken)
    {
        var statusId = await Set<OrderStatus>()
            .AsNoTracking()
            .Where(status =>
                status.OrderStatusName == "Pending" ||
                status.OrderStatusName == "?ang ch?")
            .Select(status => (int?)status.OrderStatusId)
            .FirstOrDefaultAsync(cancellationToken);

        return statusId
            ?? throw new InvalidOperationException(
                "Pending order status is not configured.");
    }

    private void AddOrderNotification(
        int userId,
        string title,
        string content)
        => Set<Notification>().Add(new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = "Order",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

    private static void RestoreStock(Order order)
    {
        foreach (var detail in order.OrderDetails)
        {
            detail.Product.UnitInStock += detail.Quantity;
            detail.Product.IsAvailable = true;
            detail.Product.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void ValidateCheckout(CheckoutRequest request)
    {
        if (request.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.UserId),
                "User id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.OrderType))
        {
            throw new ArgumentException("Order type is required.", nameof(request.OrderType));
        }

        if (request.OrderType.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Order type must not exceed 50 characters.",
                nameof(request.OrderType));
        }

        if (request.PaymentMethodId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.PaymentMethodId),
                "Payment method id must be greater than zero.");
        }
    }

    private static void ValidateOrderId(int orderId)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderId),
                "Order id must be greater than zero.");
        }
    }

    private static decimal CurrentPrice(Product product)
        => product.DiscountPrice ?? product.Price;

    private static bool RequiresOnlinePayment(string methodName)
        => methodName.Contains("transfer", StringComparison.OrdinalIgnoreCase)
        || methodName.Contains("bank", StringComparison.OrdinalIgnoreCase)
        || methodName.Contains("online", StringComparison.OrdinalIgnoreCase)
        || methodName.Contains("QR", StringComparison.OrdinalIgnoreCase)
        || methodName.Contains("Chuy?n", StringComparison.OrdinalIgnoreCase);

    private static bool IsCancelledStatus(string statusName)
        => statusName.Contains("Cancelled", StringComparison.OrdinalIgnoreCase)
        || statusName.Contains("Canceled", StringComparison.OrdinalIgnoreCase)
        || statusName.Contains("H?y", StringComparison.OrdinalIgnoreCase);

    private static bool IsPendingPaymentStatus(string statusName)
        => statusName.Contains("Pending", StringComparison.OrdinalIgnoreCase)
        || statusName.Contains("ch?", StringComparison.OrdinalIgnoreCase);

    private static bool IsFinishedStatus(string statusName)
        => IsCancelledStatus(statusName)
        || statusName.Contains("Completed", StringComparison.OrdinalIgnoreCase)
        || statusName.Contains("Ho?n", StringComparison.OrdinalIgnoreCase);

    private static OrderDto ToOrderDto(Order order)
        => new(
            order.OrderId,
            order.CustomerUserId,
            order.CustomerUser?.Name,
            order.OrderDate,
            order.TotalPrice,
            order.OrderType,
            order.Note,
            order.OrderStatusId,
            order.OrderStatus.OrderStatusName,
            order.OrderDetails
                .Select(detail => new OrderItemDto(
                    detail.OrderDetailId,
                    detail.ProductId,
                    detail.Product.Name,
                    detail.Quantity,
                    detail.UnitPrice,
                    detail.Subtotal))
                .ToList(),
            order.Payments
                .Select(payment => new PaymentDto(
                    payment.PaymentId,
                    payment.PaymentAmount,
                    payment.MethodId,
                    payment.Method.MethodName,
                    payment.PaymentStatus,
                    payment.TransactionCode,
                    payment.PaymentDate,
                    payment.PaidAt))
                .ToList());
}
