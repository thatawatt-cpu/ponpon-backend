using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Services;

public sealed class OrderShippingStatusUpdater : IOrderShippingStatusUpdater
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zort;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILineOrderNotificationService _lineNotifications;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly ILogger<OrderShippingStatusUpdater> _logger;

    public OrderShippingStatusUpdater(
        IOrderRepository orders,
        IZortOrderClient zort,
        IOrderingUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILineOrderNotificationService lineNotifications,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        ILogger<OrderShippingStatusUpdater> logger)
    {
        _orders = orders;
        _zort = zort;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lineNotifications = lineNotifications;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _logger = logger;
    }

    public async Task ApplyAsync(
        Guid orderId,
        ShippingOrderProgress progress,
        string? trackingNumber,
        DateTime? occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException($"Order {orderId} linked to a SHIPPOP shipment was not found.");
        }

        var targetStatus = MapStatus(progress);
        var currentStatus = ParseStatus(order.Status);
        if (!ShouldApply(currentStatus, targetStatus))
        {
            _logger.LogInformation(
                "Ignored SHIPPOP order status regression: OrderId={OrderId} CurrentStatus={CurrentStatus} TargetStatus={TargetStatus} OccurredAtUtc={OccurredAtUtc}",
                orderId,
                order.Status,
                targetStatus,
                occurredAtUtc);
            return;
        }

        var targetStatusName = targetStatus.ToString();
        var statusChanged = !string.Equals(order.Status, targetStatusName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(order.Status, ((int)targetStatus).ToString(), StringComparison.OrdinalIgnoreCase);
        var trackingChanged = !string.IsNullOrWhiteSpace(trackingNumber)
            && !string.Equals(order.TrackingNo, trackingNumber.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!statusChanged && !trackingChanged)
        {
            return;
        }

        if (statusChanged)
        {
            await _zort.UpdateOrderStatusAsync(order.Number, (int)targetStatus, cancellationToken);
        }

        order.ApplyShippingStatus(targetStatusName, trackingNumber, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (statusChanged)
        {
            await _lineNotifications.NotifyShippingStatusAsync(
                new LineOrderNotification(
                    order.Id,
                    order.Number,
                    order.IntegrationCustomerId,
                    order.CustomerName,
                    order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
                    Status: targetStatusName,
                    TrackingNumber: trackingNumber ?? order.TrackingNo),
                cancellationToken);
            await _shopRealtimeNotifications.NotifyAsync(
                new ShopRealtimeNotification(
                    order.CustomerId,
                    order.IntegrationCustomerId,
                    "shipping_status",
                    order.Id,
                    order.Number,
                    GetShippingTitle(targetStatus),
                    GetShippingMessage(targetStatus),
                    order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
                    targetStatusName,
                    trackingNumber ?? order.TrackingNo),
                cancellationToken);
        }
    }

    private static ZortOrderStatus MapStatus(ShippingOrderProgress progress) => progress switch
    {
        ShippingOrderProgress.Shipping => ZortOrderStatus.Shipping,
        ShippingOrderProgress.Completed => ZortOrderStatus.Success,
        ShippingOrderProgress.Failed => ZortOrderStatus.FailedShipment,
        ShippingOrderProgress.Returned => ZortOrderStatus.Returned,
        _ => throw new ArgumentOutOfRangeException(nameof(progress), progress, null)
    };

    private static ZortOrderStatus? ParseStatus(string status)
    {
        if (Enum.TryParse<ZortOrderStatus>(status, ignoreCase: true, out var named))
        {
            return named;
        }

        return int.TryParse(status, out var numeric) && Enum.IsDefined(typeof(ZortOrderStatus), numeric)
            ? (ZortOrderStatus)numeric
            : null;
    }

    private static bool ShouldApply(ZortOrderStatus? current, ZortOrderStatus target)
    {
        if (current is null || current == target)
        {
            return true;
        }

        return current switch
        {
            ZortOrderStatus.Voided => false,
            ZortOrderStatus.Success => false,
            ZortOrderStatus.Returned => false,
            _ => true
        };
    }

    private static string GetShippingTitle(ZortOrderStatus status) => status switch
    {
        ZortOrderStatus.Shipping => "พัสดุกำลังจัดส่ง",
        ZortOrderStatus.Success => "จัดส่งสำเร็จ",
        ZortOrderStatus.FailedShipment => "การจัดส่งมีปัญหา",
        ZortOrderStatus.Returned => "พัสดุกำลังตีกลับ",
        _ => "อัปเดตสถานะจัดส่ง"
    };

    private static string GetShippingMessage(ZortOrderStatus status) => status switch
    {
        ZortOrderStatus.Shipping => "พัสดุถูกรับเข้าระบบขนส่งแล้ว",
        ZortOrderStatus.Success => "พัสดุถูกจัดส่งเรียบร้อยแล้ว",
        ZortOrderStatus.FailedShipment => "ร้านค้ากำลังตรวจสอบรายการจัดส่งนี้",
        ZortOrderStatus.Returned => "พัสดุมีสถานะคืนกลับ ร้านค้ากำลังตรวจสอบ",
        _ => "มีการอัปเดตสถานะพัสดุของคุณ"
    };
}
