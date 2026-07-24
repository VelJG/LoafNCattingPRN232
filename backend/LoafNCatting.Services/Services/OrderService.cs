using System.Data;
using System.Globalization;
using System.Text;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.DTOs.Orders;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class OrderService : IOrderService
{
    private const string CustomerRoleName = "Customer";
    private const string StaffRoleName = "Staff";
    private const string AdminRoleName = "Admin";
    private const string PendingPaymentStatus = "Pending";
    private const string PaidPaymentStatus = "Paid";
    private const string CancelledPaymentStatus = "Cancelled";

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    public async Task<CheckoutOptionsDto> GetCheckoutOptionsAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var paymentMethods = await Set<PaymentMethod>()
            .AsNoTracking()
            .OrderBy(method => method.MethodId)
            .Select(method => new PaymentMethodOptionDto(
                method.MethodId,
                method.MethodName,
                method.Description))
            .ToListAsync(cancellationToken);

        return new CheckoutOptionsDto(
            ["DineIn", "Takeaway"],
            paymentMethods);
    }

    public async Task<IReadOnlyList<OrderDto>> GetMineAsync(
        int customerUserId,
        int? statusId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        ValidateOptionalStatusId(statusId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var query = OrderQuery(trackChanges: false)
            .Where(order => order.CustomerUserId == customerUserId);
        if (statusId.HasValue)
        {
            query = query.Where(order => order.OrderStatusId == statusId.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .ToListAsync(cancellationToken);
        return orders.Select(ToOrderDto).ToList();
    }

    public async Task<OrderDto> GetMineByIdAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        ValidateOrderId(orderId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);

        var order = await OrderQuery(trackChanges: false)
            .SingleOrDefaultAsync(
                current =>
                    current.OrderId == orderId &&
                    current.CustomerUserId == customerUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        return ToOrderDto(order);
    }

    public async Task<OrderDto> CheckoutAsync(
        int customerUserId,
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(customerUserId, nameof(customerUserId));
        ValidateCheckout(request);
        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        int orderId;
        try
        {
            await EnsureActiveCustomerAsync(customerUserId, cancellationToken);
            var cart = await FindCartAsync(customerUserId, cancellationToken);
            if (cart is null || cart.CartItems.Count == 0)
            {
                throw new InvalidOperationException("Cart is empty.");
            }

            var paymentMethod = await Set<PaymentMethod>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    method => method.MethodId == request.PaymentMethodId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Payment method was not found.");

            var reservation = await GetValidatedReservationAsync(
                customerUserId,
                request,
                cancellationToken);
            var tableId = request.TableId ?? reservation?.TableId;
            if (tableId.HasValue &&
                !await Set<RestaurantTable>()
                    .AsNoTracking()
                    .AnyAsync(
                        table => table.TableId == tableId.Value,
                        cancellationToken))
            {
                throw new KeyNotFoundException("Restaurant table was not found.");
            }

            var requestedItems = cart.CartItems
                .GroupBy(item => item.ProductId)
                .Select(group => new RequestedOrderItem(
                    group.Key,
                    group.Sum(item => item.Quantity)))
                .ToList();
            if (requestedItems.Any(item => item.Quantity <= 0))
            {
                throw new InvalidOperationException(
                    "Cart contains an invalid quantity.");
            }

            var productIds = requestedItems
                .Select(item => item.ProductId)
                .ToList();
            var products = cart.CartItems
                .Select(item => item.Product)
                .GroupBy(product => product.ProductId)
                .ToDictionary(group => group.Key, group => group.First());
            if (products.Count != productIds.Count)
            {
                throw new KeyNotFoundException(
                    "One or more products no longer exist.");
            }

            var now = UtcNow();
            foreach (var item in requestedItems)
            {
                var product = products[item.ProductId];
                if (!product.IsAvailable || product.UnitInStock < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product '{product.Name}'.");
                }

                product.UnitInStock -= item.Quantity;
                product.IsAvailable = product.UnitInStock > 0;
                product.UpdatedAt = now;
            }

            var pendingStatus = await GetPendingOrderStatusAsync(cancellationToken);
            var subtotal = requestedItems.Sum(
                item => CurrentPrice(products[item.ProductId]) * item.Quantity);
            var orderType = request.OrderType.Trim();
            var serviceFee = IsTakeaway(orderType)
                ? 0m
                : Math.Round(subtotal * 0.05m, 0);

            var order = new Order
            {
                CustomerUserId = customerUserId,
                TableId = tableId,
                ReservationId = request.ReservationId,
                OrderType = orderType,
                Note = string.IsNullOrWhiteSpace(request.Note)
                    ? null
                    : request.Note.Trim(),
                OrderStatusId = pendingStatus.OrderStatusId,
                OrderStatus = pendingStatus,
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
                    Product = product,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = unitPrice * item.Quantity
                });
            }

            var requiresOnlinePayment = RequiresOnlinePayment(
                paymentMethod.MethodName);
            order.Payments.Add(new Payment
            {
                MethodId = paymentMethod.MethodId,
                PaymentAmount = order.TotalPrice,
                PaymentStatus = requiresOnlinePayment
                    ? PendingPaymentStatus
                    : PaidPaymentStatus,
                TransactionCode = requiresOnlinePayment
                    ? null
                    : $"POS-{now:yyyyMMddHHmmssfff}",
                PaymentDate = now,
                PaidAt = requiresOnlinePayment ? null : now
            });

            Set<Order>().Add(order);
            Set<CartItem>().RemoveRange(cart.CartItems.ToList());
            cart.CartItems.Clear();
            cart.UpdatedAt = now;

            await _notificationService.QueueForUserAsync(
                customerUserId,
                new NotificationDraft(
                    "Order created",
                    "Your order has been created and is waiting for the store to process it.",
                    NotificationTypes.OrderCreated),
                cancellationToken);
            await _notificationService.QueueForActiveStaffAsync(
                new NotificationDraft(
                    "New order",
                    $"A new {orderType} order totaling {order.TotalPrice:N0} VND is waiting for processing.",
                    NotificationTypes.OrderCreated),
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            orderId = order.OrderId;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return await GetMineByIdAsync(
            customerUserId,
            orderId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<OrderDto>> GetForStoreAsync(
        int operatorUserId,
        int? statusId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        ValidateOptionalStatusId(statusId);
        await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);

        var query = OrderQuery(trackChanges: false);
        if (statusId.HasValue)
        {
            query = query.Where(order => order.OrderStatusId == statusId.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.OrderId)
            .ToListAsync(cancellationToken);
        return orders.Select(ToOrderDto).ToList();
    }

    public async Task<OrderDto> GetForStoreByIdAsync(
        int operatorUserId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        ValidateOrderId(orderId);
        await EnsureActiveStoreOperatorAsync(operatorUserId, cancellationToken);

        var order = await OrderQuery(trackChanges: false)
            .SingleOrDefaultAsync(
                current => current.OrderId == orderId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        return ToOrderDto(order);
    }

    public async Task<OrderDto> UpdateStatusByStoreAsync(
        int operatorUserId,
        int orderId,
        OrderStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(operatorUserId, nameof(operatorUserId));
        ValidateOrderId(orderId);
        if (request.OrderStatusId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.OrderStatusId),
                "Order status id must be greater than zero.");
        }

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            await EnsureActiveStoreOperatorAsync(
                operatorUserId,
                cancellationToken);
            var targetStatus = await Set<OrderStatus>()
                .SingleOrDefaultAsync(
                    status => status.OrderStatusId == request.OrderStatusId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Order status was not found.");
            var order = await OrderQuery(trackChanges: true)
                .SingleOrDefaultAsync(
                    current => current.OrderId == orderId,
                    cancellationToken)
                ?? throw new KeyNotFoundException("Order was not found.");

            if (order.OrderStatusId != targetStatus.OrderStatusId)
            {
                EnsureAllowedTransition(
                    order.OrderStatus.OrderStatusName,
                    targetStatus.OrderStatusName);
                var targetState = ClassifyOrderStatus(
                    targetStatus.OrderStatusName);
                if (targetState == OrderState.Cancelled)
                {
                    RestoreStock(order);
                    foreach (var payment in order.Payments.Where(
                                 current => IsPendingPaymentStatus(
                                     current.PaymentStatus)))
                    {
                        payment.PaymentStatus = CancelledPaymentStatus;
                    }
                }

                order.OrderStatusId = targetStatus.OrderStatusId;
                order.OrderStatus = targetStatus;
                order.StaffUserId = operatorUserId;
                order.UpdatedAt = UtcNow();

                if (order.CustomerUserId.HasValue &&
                    order.CustomerUser?.IsActive == true)
                {
                    await _notificationService.QueueForUserAsync(
                        order.CustomerUserId.Value,
                        new NotificationDraft(
                            targetState == OrderState.Cancelled
                                ? "Order cancelled"
                                : "Order status updated",
                            $"Order #{order.OrderId} is now {targetStatus.OrderStatusName}.",
                            targetState == OrderState.Cancelled
                                ? NotificationTypes.OrderCancelled
                                : NotificationTypes.OrderStatusChanged),
                        cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return await GetForStoreByIdAsync(
            operatorUserId,
            orderId,
            cancellationToken);
    }

    private DbSet<T> Set<T>() where T : class
        => _unitOfWork.Repository<T>().Entities;

    private IQueryable<Order> OrderQuery(bool trackChanges)
    {
        IQueryable<Order> query = Set<Order>()
            .Include(order => order.CustomerUser)
            .Include(order => order.StaffUser)
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
            .SingleOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);

    private async Task<Reservation?> GetValidatedReservationAsync(
        int customerUserId,
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ReservationId.HasValue)
        {
            return null;
        }

        var reservation = await Set<Reservation>()
            .AsNoTracking()
            .Include(current => current.Status)
            .SingleOrDefaultAsync(
                current => current.ReservationId == request.ReservationId.Value,
                cancellationToken)
            ?? throw new KeyNotFoundException("Reservation was not found.");

        if (reservation.UserId != customerUserId)
        {
            throw new InvalidOperationException(
                "Reservation does not belong to the authenticated customer.");
        }

        if (IsTerminalReservationStatus(reservation.Status.StatusName))
        {
            throw new InvalidOperationException(
                "Orders cannot be created for a finished reservation.");
        }

        if (request.TableId.HasValue &&
            request.TableId.Value != reservation.TableId)
        {
            throw new InvalidOperationException(
                "Table does not match the reservation.");
        }

        return reservation;
    }

    private async Task<OrderStatus> GetPendingOrderStatusAsync(
        CancellationToken cancellationToken)
    {
        var statuses = await Set<OrderStatus>()
            .OrderBy(status => status.OrderStatusId)
            .ToListAsync(cancellationToken);
        return statuses.FirstOrDefault(
                status => ClassifyOrderStatus(status.OrderStatusName) ==
                    OrderState.Pending)
            ?? throw new InvalidOperationException(
                "A pending order status is not configured.");
    }

    private async Task EnsureActiveCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken)
    {
        var isActiveCustomer = await Set<User>()
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.UserId == customerUserId &&
                    user.IsActive &&
                    user.Role.RoleName == CustomerRoleName,
                cancellationToken);
        if (!isActiveCustomer)
        {
            throw new UnauthorizedAccessException(
                "The authenticated customer account is not active or valid.");
        }
    }

    private async Task EnsureActiveStoreOperatorAsync(
        int operatorUserId,
        CancellationToken cancellationToken)
    {
        var isActiveOperator = await Set<User>()
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.UserId == operatorUserId &&
                    user.IsActive &&
                    (user.Role.RoleName == StaffRoleName ||
                     user.Role.RoleName == AdminRoleName),
                cancellationToken);
        if (!isActiveOperator)
        {
            throw new UnauthorizedAccessException(
                "The authenticated store operator account is not active or valid.");
        }
    }

    private void RestoreStock(Order order)
    {
        var now = UtcNow();
        foreach (var detail in order.OrderDetails)
        {
            detail.Product.UnitInStock += detail.Quantity;
            detail.Product.IsAvailable = true;
            detail.Product.UpdatedAt = now;
        }
    }

    private static void EnsureAllowedTransition(
        string currentStatusName,
        string targetStatusName)
    {
        var current = ClassifyOrderStatus(currentStatusName);
        var target = ClassifyOrderStatus(targetStatusName);
        var isAllowed = target == OrderState.Cancelled &&
                current is OrderState.Pending or OrderState.Processing or OrderState.Ready
            || current == OrderState.Pending && target == OrderState.Processing
            || current == OrderState.Processing &&
                target is OrderState.Ready or OrderState.Completed
            || current == OrderState.Ready && target == OrderState.Completed;

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                $"Order status cannot change from '{currentStatusName}' to '{targetStatusName}'.");
        }
    }

    private static OrderState ClassifyOrderStatus(string statusName)
    {
        var normalized = NormalizeText(statusName);
        if (ContainsAny(normalized, "cancel", "huy"))
        {
            return OrderState.Cancelled;
        }

        if (ContainsAny(normalized, "complete", "hoan thanh"))
        {
            return OrderState.Completed;
        }

        if (ContainsAny(normalized, "ready", "san sang"))
        {
            return OrderState.Ready;
        }

        if (ContainsAny(
                normalized,
                "process",
                "pha che",
                "chuan bi",
                "dang lam"))
        {
            return OrderState.Processing;
        }

        if (ContainsAny(normalized, "pending", "cho xu ly", "dang cho"))
        {
            return OrderState.Pending;
        }

        return OrderState.Unknown;
    }

    private static bool IsTerminalReservationStatus(string statusName)
    {
        var normalized = NormalizeText(statusName);
        return ContainsAny(
            normalized,
            "cancel",
            "huy",
            "complete",
            "hoan thanh",
            "expired",
            "het han",
            "no show",
            "khong den");
    }

    private static bool IsPendingPaymentStatus(string statusName)
    {
        var normalized = NormalizeText(statusName);
        return ContainsAny(normalized, "pending", "dang cho", "cho thanh toan");
    }

    private static bool RequiresOnlinePayment(string methodName)
    {
        var normalized = NormalizeText(methodName);
        return ContainsAny(
            normalized,
            "transfer",
            "bank",
            "online",
            "qr",
            "chuyen khoan");
    }

    private static bool IsTakeaway(string orderType)
    {
        var normalized = NormalizeText(orderType).Replace(" ", string.Empty);
        return normalized is "takeaway" or "mangdi";
    }

    private static bool ContainsAny(string value, params string[] candidates)
        => candidates.Any(candidate =>
            value.Contains(candidate, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeText(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static void ValidateCheckout(CheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OrderType))
        {
            throw new ArgumentException(
                "Order type is required.",
                nameof(request));
        }

        if (request.OrderType.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Order type must not exceed 50 characters.",
                nameof(request));
        }

        if (request.TableId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TableId),
                "Table id must be greater than zero.");
        }

        if (request.ReservationId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.ReservationId),
                "Reservation id must be greater than zero.");
        }

        if (request.PaymentMethodId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.PaymentMethodId),
                "Payment method id must be greater than zero.");
        }
    }

    private static void ValidateOptionalStatusId(int? statusId)
    {
        if (statusId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusId),
                "Status id must be greater than zero.");
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

    private static void ValidateUserId(int userId, string parameterName)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "User id must be greater than zero.");
        }
    }

    private DateTime UtcNow()
        => _timeProvider.GetUtcNow().UtcDateTime;

    private static decimal CurrentPrice(Product product)
        => product.DiscountPrice ?? product.Price;

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
                .OrderBy(detail => detail.OrderDetailId)
                .Select(detail => new OrderItemDto(
                    detail.OrderDetailId,
                    detail.ProductId,
                    detail.Product.Name,
                    detail.Quantity,
                    detail.UnitPrice,
                    detail.Subtotal))
                .ToList(),
            order.Payments
                .OrderBy(payment => payment.PaymentId)
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

    private sealed record RequestedOrderItem(int ProductId, int Quantity);

    private enum OrderState
    {
        Unknown,
        Pending,
        Processing,
        Ready,
        Completed,
        Cancelled
    }
}
