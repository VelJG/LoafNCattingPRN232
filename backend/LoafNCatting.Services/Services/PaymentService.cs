using System.Data;
using System.Globalization;
using System.Text;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoafNCatting.Services.Services;

public sealed class PaymentService : IPaymentService
{
    private const string CustomerRoleName = "Customer";
    private const string PendingPaymentStatus = "Pending";
    private const string PaidPaymentStatus = "Paid";
    private const string CancelledPaymentStatus = "Cancelled";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;
    private readonly PayOsSettings _payOsSettings;
    private readonly PaymentSettings _paymentSettings;
    private readonly TimeProvider _timeProvider;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        INotificationService notificationService,
        IOptions<PayOsSettings> payOsSettings,
        IOptions<PaymentSettings> paymentSettings,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
        _payOsSettings = payOsSettings.Value;
        _paymentSettings = paymentSettings.Value;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentLinkDto> CreatePaymentLinkAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(customerUserId, orderId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);
        var order = await GetOwnedOrderAsync(
            customerUserId,
            orderId,
            trackChanges: false,
            cancellationToken);
        var payment = GetPrimaryPayment(order);

        if (await ExpireIfNeededAsync(
                customerUserId,
                orderId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The pending payment has expired and the order was cancelled.");
        }

        EnsurePaymentLinkCanBeCreated(order, payment);
        var amount = ToVndAmount(order.TotalPrice);
        var orderCode = _timeProvider
            .GetUtcNow()
            .ToUnixTimeMilliseconds();
        var description = $"LNC order {order.OrderId}";
        if (description.Length > 25)
        {
            description = description[..25];
        }

        var gatewayRequest = new PaymentGatewayRequest(
            orderCode,
            amount,
            description,
            order.OrderDetails
                .OrderBy(detail => detail.OrderDetailId)
                .Select(detail => new PaymentGatewayItem(
                    detail.Product.Name,
                    detail.Quantity,
                    ToVndAmount(detail.UnitPrice)))
                .ToList(),
            RequiredUrl(
                _payOsSettings.CancelUrl,
                "PayOS:CancelUrl"),
            RequiredUrl(
                _payOsSettings.ReturnUrl,
                "PayOS:ReturnUrl"));
        var link = await _paymentGateway.CreatePaymentLinkAsync(
            gatewayRequest,
            cancellationToken);

        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var current = await GetOwnedOrderAsync(
                customerUserId,
                orderId,
                trackChanges: true,
                cancellationToken);
            var currentPayment = GetPrimaryPayment(current);
            EnsurePaymentLinkCanBeCreated(current, currentPayment);
            currentPayment.TransactionCode = link.OrderCode.ToString(
                CultureInfo.InvariantCulture);
            currentPayment.PaymentStatus = PendingPaymentStatus;
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(
                CancellationToken.None);
            throw;
        }

        return new PaymentLinkDto(
            orderId,
            link.OrderCode,
            link.Amount,
            link.CheckoutUrl,
            link.QrCode,
            link.PaymentLinkId);
    }

    public async Task<PaymentStatusDto> GetPaymentStatusAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(customerUserId, orderId);
        await EnsureActiveCustomerAsync(customerUserId, cancellationToken);
        var order = await GetOwnedOrderAsync(
            customerUserId,
            orderId,
            trackChanges: false,
            cancellationToken);
        var payment = GetPrimaryPayment(order);

        if (IsPaid(payment.PaymentStatus))
        {
            return ToStatus(order, payment, isPaid: true);
        }

        if (IsCancelled(payment.PaymentStatus))
        {
            return ToStatus(order, payment, isPaid: false);
        }

        if (await ExpireIfNeededAsync(
                customerUserId,
                orderId,
                cancellationToken))
        {
            return await GetCurrentStatusAsync(
                customerUserId,
                orderId,
                cancellationToken);
        }

        if (!long.TryParse(
                payment.TransactionCode,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var orderCode))
        {
            return ToStatus(order, payment, isPaid: false);
        }

        var gatewayStatus = await _paymentGateway.GetPaymentStatusAsync(
            orderCode,
            cancellationToken);
        var normalizedStatus = gatewayStatus.Status.Trim().ToUpperInvariant();
        if (normalizedStatus == "PAID")
        {
            await MarkPaidAsync(
                customerUserId,
                orderId,
                cancellationToken);
        }
        else if (normalizedStatus is "CANCELLED" or "EXPIRED")
        {
            await CancelPendingOrderAsync(
                customerUserId,
                orderId,
                requireExpired: false,
                cancellationToken);
        }
        else
        {
            await ExpireIfNeededAsync(
                customerUserId,
                orderId,
                cancellationToken);
        }

        return await GetCurrentStatusAsync(
            customerUserId,
            orderId,
            cancellationToken);
    }

    private async Task<PaymentStatusDto> GetCurrentStatusAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken)
    {
        var order = await GetOwnedOrderAsync(
            customerUserId,
            orderId,
            trackChanges: false,
            cancellationToken);
        var payment = GetPrimaryPayment(order);
        return ToStatus(order, payment, IsPaid(payment.PaymentStatus));
    }

    private async Task MarkPaidAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var order = await GetOwnedOrderAsync(
                customerUserId,
                orderId,
                trackChanges: true,
                cancellationToken);
            var payment = GetPrimaryPayment(order);
            if (IsPending(payment.PaymentStatus) &&
                IsPendingOrder(order.OrderStatus.OrderStatusName))
            {
                payment.PaymentStatus = PaidPaymentStatus;
                payment.PaidAt = UtcNow();
                order.UpdatedAt = UtcNow();
                await _notificationService.QueueForUserAsync(
                    customerUserId,
                    new NotificationDraft(
                        "Payment successful",
                        $"Payment for order #{orderId} was completed successfully.",
                        NotificationTypes.PaymentSucceeded),
                    cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return;
            }

            await _unitOfWork.RollbackTransactionAsync(
                CancellationToken.None);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(
                CancellationToken.None);
            throw;
        }
    }

    private Task<bool> ExpireIfNeededAsync(
        int customerUserId,
        int orderId,
        CancellationToken cancellationToken)
        => CancelPendingOrderAsync(
            customerUserId,
            orderId,
            requireExpired: true,
            cancellationToken);

    private async Task<bool> CancelPendingOrderAsync(
        int customerUserId,
        int orderId,
        bool requireExpired,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var order = await GetOwnedOrderAsync(
                customerUserId,
                orderId,
                trackChanges: true,
                cancellationToken);
            var payment = GetPrimaryPayment(order);
            var canCancel = IsPending(payment.PaymentStatus) &&
                IsPendingOrder(order.OrderStatus.OrderStatusName) &&
                (!requireExpired || IsExpired(order));
            if (!canCancel)
            {
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);
                return false;
            }

            var orderStatuses = await Set<OrderStatus>()
                .ToListAsync(cancellationToken);
            var cancelledStatus = orderStatuses
                .SingleOrDefault(status => IsCancelledOrder(
                    status.OrderStatusName))
                ?? throw new InvalidOperationException(
                    "A cancelled order status is not configured.");
            RestoreStock(order);
            payment.PaymentStatus = CancelledPaymentStatus;
            order.OrderStatusId = cancelledStatus.OrderStatusId;
            order.OrderStatus = cancelledStatus;
            order.UpdatedAt = UtcNow();
            await _notificationService.QueueForUserAsync(
                customerUserId,
                new NotificationDraft(
                    "Payment not completed",
                    $"Payment for order #{orderId} was cancelled or expired.",
                    NotificationTypes.PaymentCancelled),
                cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(
                CancellationToken.None);
            throw;
        }
    }

    private async Task<Order> GetOwnedOrderAsync(
        int customerUserId,
        int orderId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = Set<Order>()
            .Include(order => order.OrderStatus)
            .Include(order => order.OrderDetails)
            .ThenInclude(detail => detail.Product)
            .Include(order => order.Payments)
            .ThenInclude(payment => payment.Method)
            .Where(order =>
                order.OrderId == orderId &&
                order.CustomerUserId == customerUserId);
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
    }

    private async Task EnsureActiveCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken)
    {
        var isActive = await Set<User>()
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.UserId == customerUserId &&
                    user.IsActive &&
                    user.Role.RoleName == CustomerRoleName,
                cancellationToken);
        if (!isActive)
        {
            throw new UnauthorizedAccessException(
                "The authenticated customer account is not active or valid.");
        }
    }

    private bool IsExpired(Order order)
    {
        var minutes = _paymentSettings.PendingPaymentExpiryMinutes > 0
            ? _paymentSettings.PendingPaymentExpiryMinutes
            : 15;
        var startedAt = order.OrderDate != default
            ? order.OrderDate
            : order.CreatedAt;
        return UtcNow() - NormalizeUtc(startedAt) >=
            TimeSpan.FromMinutes(minutes);
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

    private static Payment GetPrimaryPayment(Order order)
        => order.Payments
            .OrderBy(payment => payment.PaymentId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "The order does not have a payment record.");

    private static void EnsurePaymentLinkCanBeCreated(
        Order order,
        Payment payment)
    {
        if (!IsPending(payment.PaymentStatus) ||
            !IsPendingOrder(order.OrderStatus.OrderStatusName))
        {
            throw new InvalidOperationException(
                "A payment link can only be created for an order awaiting online payment.");
        }
    }

    private static PaymentStatusDto ToStatus(
        Order order,
        Payment payment,
        bool isPaid)
        => new(
            order.OrderId,
            payment.PaymentStatus,
            order.OrderStatus.OrderStatusName,
            isPaid);

    private static int ToVndAmount(decimal amount)
    {
        var rounded = Math.Round(
            amount,
            0,
            MidpointRounding.AwayFromZero);
        if (rounded <= 0 || rounded > int.MaxValue)
        {
            throw new InvalidOperationException(
                "The payment amount is outside the supported PayOS range.");
        }

        return decimal.ToInt32(rounded);
    }

    private static string RequiredUrl(string value, string settingName)
        => Uri.TryCreate(value, UriKind.Absolute, out _)
            ? value
            : throw new InvalidOperationException(
                $"{settingName} must be an absolute URL.");

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc),
            _ => value
        };

    private static bool IsPending(string value)
        => ContainsAny(
            NormalizeText(value),
            "pending",
            "dang cho",
            "cho thanh toan");

    private static bool IsPaid(string value)
        => ContainsAny(
            NormalizeText(value),
            "paid",
            "da thanh toan");

    private static bool IsCancelled(string value)
        => ContainsAny(
            NormalizeText(value),
            "cancel",
            "huy",
            "expired",
            "het han");

    private static bool IsPendingOrder(string value)
        => ContainsAny(
            NormalizeText(value),
            "pending",
            "dang cho",
            "cho xu ly");

    private static bool IsCancelledOrder(string value)
        => ContainsAny(
            NormalizeText(value),
            "cancel",
            "huy");

    private static bool ContainsAny(
        string value,
        params string[] candidates)
        => candidates.Any(candidate =>
            value.Contains(
                candidate,
                StringComparison.OrdinalIgnoreCase));

    private static string NormalizeText(string value)
    {
        var decomposed = value.Trim().Normalize(
            NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(
            NormalizationForm.FormC);
    }

    private static void ValidateIds(
        int customerUserId,
        int orderId)
    {
        if (customerUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerUserId),
                "Customer user id must be greater than zero.");
        }

        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderId),
                "Order id must be greater than zero.");
        }
    }

    private DbSet<T> Set<T>() where T : class
        => _unitOfWork.Repository<T>().Entities;

    private DateTime UtcNow()
        => _timeProvider.GetUtcNow().UtcDateTime;
}
