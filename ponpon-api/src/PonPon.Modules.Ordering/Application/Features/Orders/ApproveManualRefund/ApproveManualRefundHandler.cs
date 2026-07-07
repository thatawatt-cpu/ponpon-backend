using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using PonPon.Modules.Ordering.Application.Services;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ApproveManualRefund;

public sealed class ApproveManualRefundHandler
{
    private const int MaxReasonLength = 2000;
    private const int MaxRefundReferenceLength = 100;

    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IShippingBookingAutomation _shippingBooking;
    private readonly ILineOrderNotificationService _lineNotifications;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IDateTimeProvider _clock;
    private readonly OrderStockReservationService _stockReservations;

    public ApproveManualRefundHandler(
        IOrderRepository orders,
        IZortOrderClient zortClient,
        IOrderingUnitOfWork unitOfWork,
        IShippingBookingAutomation shippingBooking,
        ILineOrderNotificationService lineNotifications,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        OrderStockReservationService stockReservations,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _zortClient = zortClient;
        _unitOfWork = unitOfWork;
        _shippingBooking = shippingBooking;
        _lineNotifications = lineNotifications;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _stockReservations = stockReservations;
        _clock = clock;
    }

    public async Task HandleAsync(
        ApproveManualRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        await using var paymentLock = await _orders.AcquirePaymentLockAsync(command.OrderId, cancellationToken);
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        if (string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase)
            || string.Equals(order.PaymentStatus, "Voided", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!IsPaid(order.PaymentStatus))
        {
            throw new BadRequestException("Only paid orders can be approved for manual refund.");
        }

        await _shippingBooking.CancelBookingForOrderAsync(
            order.Id,
            command.Reason,
            cancellationToken);

        var now = _clock.UtcNow;
        var refundedAmount = ManualRefundAmountCalculator.Calculate(
            order.PaymentAmount,
            order.RefundedAmount,
            command.RefundedAmount);
        var refundReference = BuildManualRefundReference(command.RefundReference);

        order.RecordOmiseRefund(
            refundReference,
            OrderRefundStatus.ManualRefunded,
            order.RefundedAmount + refundedAmount,
            now);

        await _zortClient.VoidOrderAsync(order.ZortOrderId, cancellationToken);

        order.MarkVoided(now, command.Reason, "AdminManualRefund");
        await _stockReservations.ReleaseAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await paymentLock.CompleteAsync(cancellationToken);

        await _lineNotifications.NotifyManualRefundCompletedAsync(
            new LineOrderNotification(
                order.Id,
                order.Number,
                order.IntegrationCustomerId,
                order.CustomerName,
                refundedAmount,
                Reason: command.Reason),
            cancellationToken);

        await _shopRealtimeNotifications.NotifyAsync(
            new ShopRealtimeNotification(
                order.CustomerId,
                order.IntegrationCustomerId,
                "refund_completed",
                order.Id,
                order.Number,
                "คืนเงินเรียบร้อยแล้ว",
                "ร้านค้าดำเนินการคืนเงินและยกเลิกคำสั่งซื้อเรียบร้อยแล้ว",
                refundedAmount,
                OrderRefundStatus.ManualRefunded),
            cancellationToken);
    }

    private static void Validate(ApproveManualRefundCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Reason)
            || command.Reason.Trim().Length > MaxReasonLength)
        {
            throw new BadRequestException(
                $"Cancellation reason is required and must not exceed {MaxReasonLength} characters.");
        }

        if (command.RefundReference?.Trim().Length > MaxRefundReferenceLength)
        {
            throw new BadRequestException(
                $"Refund reference must not exceed {MaxRefundReferenceLength} characters.");
        }

        if (command.RefundedAmount is not null and <= 0)
        {
            throw new BadRequestException("Refunded amount must be greater than zero.");
        }
    }

    private static bool IsPaid(string paymentStatus)
        => string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(paymentStatus, "1", StringComparison.OrdinalIgnoreCase);

    private static string BuildManualRefundReference(string? refundReference)
    {
        var reference = refundReference?.Trim();
        return string.IsNullOrWhiteSpace(reference)
            ? "manual"
            : $"manual:{reference}";
    }
}
