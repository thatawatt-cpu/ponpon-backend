using PonPon.Modules.Ordering.Application;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using PonPon.Modules.Ordering.Application.Services;

namespace PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;

public sealed class CancelMyOrderHandler
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IShippingBookingAutomation _shippingBooking;
    private readonly IOrderPaymentRefundService _paymentRefund;
    private readonly ILineOrderNotificationService _lineNotifications;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IOrderCancellationNotifier _cancellationNotifier;
    private readonly IDateTimeProvider _clock;
    private readonly OrderStockReservationService _stockReservations;

    public CancelMyOrderHandler(
        IOrderRepository orders,
        IZortOrderClient zortClient,
        IOrderingUnitOfWork unitOfWork,
        IShippingBookingAutomation shippingBooking,
        IOrderPaymentRefundService paymentRefund,
        ILineOrderNotificationService lineNotifications,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        IOrderCancellationNotifier cancellationNotifier,
        OrderStockReservationService stockReservations,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _zortClient = zortClient;
        _unitOfWork = unitOfWork;
        _shippingBooking = shippingBooking;
        _paymentRefund = paymentRefund;
        _lineNotifications = lineNotifications;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _cancellationNotifier = cancellationNotifier;
        _stockReservations = stockReservations;
        _clock = clock;
    }

    public async Task HandleAsync(CancelMyOrderCommand command, CancellationToken cancellationToken = default)
    {
        ValidateReason(command.Reason);

        var order = await _orders.GetCustomerOrderByIdAsync(command.OrderId, command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(
                order.OmiseRefundStatus,
                OrderRefundStatus.ManualRefundPending,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (IsPackedOrLater(order.Status))
        {
            throw new BadRequestException("Order cannot be refunded after it has been packed.");
        }

        if (await _shippingBooking.HasShipmentForOrderAsync(order.Id, cancellationToken))
        {
            throw new BadRequestException("Order cannot be refunded because shipping booking already exists.");
        }

        try
        {
            await _paymentRefund.RefundOrderAsync(order.Id, cancellationToken);
        }
        catch (BadRequestException ex) when (IsManualRefundRequired(ex))
        {
            order.RequestManualRefund(command.Reason, "Customer", _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _lineNotifications.NotifyManualRefundRequestedAsync(
                CreateLineNotification(order, command.Reason),
                cancellationToken);
            await _cancellationNotifier.NotifyCustomerCancellationRequiresManualRefundAsync(
                CreateCancellationNotification(order, command.Reason),
                cancellationToken);
            await _shopRealtimeNotifications.NotifyAsync(
                CreateShopNotification(
                    order,
                    "refund_requested",
                    "ได้รับคำขอคืนเงินแล้ว",
                    "ร้านค้าจะตรวจสอบและดำเนินการคืนเงินให้",
                    OrderRefundStatus.ManualRefundPending),
                cancellationToken);
            return;
        }

        if (order.ZortOrderId > 0)
        {
            await _zortClient.VoidOrderAsync(order.ZortOrderId, cancellationToken);
        }

        order.MarkVoided(_clock.UtcNow, command.Reason, "Customer");
        await _stockReservations.ReleaseAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _lineNotifications.NotifyAutoRefundCompletedAsync(
            CreateLineNotification(order, command.Reason),
            cancellationToken);
        await _shopRealtimeNotifications.NotifyAsync(
            CreateShopNotification(
                order,
                "refund_completed",
                "คืนเงินเรียบร้อยแล้ว",
                "ระบบดำเนินการคืนเงินและยกเลิกคำสั่งซื้อเรียบร้อยแล้ว",
                order.OmiseRefundStatus),
            cancellationToken);
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000)
        {
            throw new BadRequestException("Cancellation reason is required and must not exceed 2000 characters.");
        }
    }

    private static bool IsPackedOrLater(string status)
    {
        if (string.Equals(status, "Packed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Shipping", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Returned", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "FailedShipment", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(status, out var value)
            && (value == (int)ZortOrderStatus.Success || value >= (int)ZortOrderStatus.Packed);
    }

    private static bool IsManualRefundRequired(BadRequestException exception)
        => exception.Message.Contains(
            "Payment could not be refunded automatically by Omise",
            StringComparison.OrdinalIgnoreCase);

    private static LineOrderNotification CreateLineNotification(
        Order order,
        string reason)
        => new(
            order.Id,
            order.Number,
            order.IntegrationCustomerId,
            order.CustomerName,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            Reason: reason);

    private static OrderCancellationNotification CreateCancellationNotification(
        Order order,
        string reason)
        => new(
            order.Id,
            order.Number,
            order.CustomerName,
            order.CustomerPhone,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            order.PaymentStatus,
            order.OmiseChargeId,
            reason);

    private static ShopRealtimeNotification CreateShopNotification(
        Order order,
        string type,
        string title,
        string message,
        string? status)
        => new(
            order.CustomerId,
            order.IntegrationCustomerId,
            type,
            order.Id,
            order.Number,
            title,
            message,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            status);
}
