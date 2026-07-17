using Microsoft.Extensions.Logging;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Services;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using System.Text;

namespace PonPon.Modules.Ordering.Application.Features.Orders.HandleZortWebhook;

public sealed class HandleZortWebhookHandler
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IProductRepository _products;
    private readonly IShippingBookingAutomation _shippingBooking;
    private readonly ILineOrderNotificationService _lineNotifications;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<HandleZortWebhookHandler> _logger;
    private readonly OrderStockReservationService _stockReservations;

    public HandleZortWebhookHandler(
        IOrderRepository orders,
        IZortOrderClient zortClient,
        IOrderingUnitOfWork unitOfWork,
        IProductRepository products,
        IShippingBookingAutomation shippingBooking,
        ILineOrderNotificationService lineNotifications,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        OrderStockReservationService stockReservations,
        IDateTimeProvider clock,
        ILogger<HandleZortWebhookHandler> logger)
    {
        _orders = orders;
        _zortClient = zortClient;
        _unitOfWork = unitOfWork;
        _products = products;
        _shippingBooking = shippingBooking;
        _lineNotifications = lineNotifications;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _stockReservations = stockReservations;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(HandleZortWebhookCommand command, CancellationToken cancellationToken = default)
    {
        var zortOrder = await _zortClient.GetOrderDetailAsync(command.ZortOrderId, cancellationToken);
        var snapshot = ZortOrderMapper.ToSnapshot(zortOrder);

        if (!string.Equals(snapshot.SalesChannel, SyncOrdersFromZort.SyncOrdersFromZortHandler.LineLiffSalesChannel, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Zort {Method}: skipping order ZortOrderId={ZortOrderId} from channel={SalesChannel}", command.Method, command.ZortOrderId, snapshot.SalesChannel);
            return;
        }

        var order = await _orders.GetByZortOrderIdAsync(snapshot.ZortOrderId, cancellationToken);
        var wasDelivered = order is not null && IsDelivered(order.Status);
        var wasPacked = order is not null && IsPacked(order);
        if (order is null)
        {
            order = Order.CreateFromZort(snapshot, _clock.UtcNow);
            await _orders.AddAsync(order, cancellationToken);
            _logger.LogInformation("Zort {Method}: created order ZortOrderId={ZortOrderId} Number={Number}", command.Method, command.ZortOrderId, snapshot.Number);
        }
        else
        {
            order.ApplyZortSnapshot(snapshot, _clock.UtcNow);
            _logger.LogInformation("Zort {Method}: updated order ZortOrderId={ZortOrderId} Number={Number} Status={Status}", command.Method, command.ZortOrderId, snapshot.Number, snapshot.Status);
        }

        if (IsVoided(order.Status))
            await _stockReservations.ReleaseAsync(order, cancellationToken);

        var shouldNotifyDelivered = IsDelivered(order.Status)
            && order.DeliveredNotificationSentAtUtc is null;
        var canNotifyLine = !string.IsNullOrWhiteSpace(order.IntegrationCustomerId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!wasPacked && IsPacked(order))
        {
            await _shopRealtimeNotifications.NotifyAsync(
                CreateShopNotification(
                    order,
                    "packed",
                    "กำลังเตรียมจัดส่ง",
                    "ร้านค้าแพ็คสินค้าและกำลังเตรียมส่งให้ขนส่ง",
                    ZortOrderStatus.Packed.ToString()),
                cancellationToken);
        }

        if ((!wasDelivered && IsDelivered(order.Status)) || shouldNotifyDelivered)
        {
            await _lineNotifications.NotifyShippingStatusAsync(
                CreateLineNotification(order, ZortOrderStatus.Success.ToString(), order.TrackingNo),
                cancellationToken);
            if (shouldNotifyDelivered && canNotifyLine)
            {
                order.MarkDeliveredNotificationSent(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            await _shopRealtimeNotifications.NotifyAsync(
                CreateShopNotification(
                    order,
                    "shipping_status",
                    "จัดส่งสำเร็จ",
                    "พัสดุถูกจัดส่งเรียบร้อยแล้ว",
                    ZortOrderStatus.Success.ToString(),
                    order.TrackingNo),
                cancellationToken);
        }

        if (IsPacked(order) && IsPaid(order))
        {
            var booking = await TryCreateShippingBookingAsync(order, cancellationToken);
            if (booking is not null)
            {
                await _shopRealtimeNotifications.NotifyAsync(
                    CreateShopNotification(
                        order,
                        "shipping_booked",
                        "สร้างรายการจัดส่งแล้ว",
                        "ระบบสร้าง booking ขนส่งเรียบร้อยแล้ว",
                        "booking",
                        booking.CourierTrackingCode ?? booking.TrackingCode),
                    cancellationToken);
            }
        }
    }

    private async Task<ShippingBookingResult?> TryCreateShippingBookingAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var request = await BuildShippingBookingRequestAsync(order, cancellationToken);
           if (request is null)
            {
                return null;
            }

            var booking = await _shippingBooking.CreateBookingForPackedOrderAsync(request, cancellationToken);

            _logger.LogInformation(
                "SHIPPOP booking auto-created for packed order OrderId={OrderId} Number={Number}",
                order.Id,
                order.Number);

            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to auto-create SHIPPOP booking for packed order OrderId={OrderId} Number={Number}",
                order.Id,
                order.Number);
            return null;
        }
    }

    private async Task<ShippingBookingRequest?> BuildShippingBookingRequestAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        if (await _shippingBooking.HasShipmentForOrderAsync(order.Id, cancellationToken))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(order.ShippingChannel))
        {
            _logger.LogWarning(
                "Packed order {OrderNumber} cannot create SHIPPOP booking because ShippingChannel is empty.",
                order.Number);
            return null;
        }

        var parsedAddress = ParseShippingAddress(order.ShippingAddress);
        if (parsedAddress is null)
        {
            _logger.LogWarning(
                "Packed order {OrderNumber} cannot create SHIPPOP booking because ShippingAddress is not parseable.",
                order.Number);
            return null;
        }

        var productIds = order.Items
            .Where(x => x.ProductId.HasValue)
            .Select(x => x.ProductId!.Value)
            .ToHashSet();

        if (productIds.Count == 0)
        {
            _logger.LogWarning(
                "Packed order {OrderNumber} cannot create SHIPPOP booking because order items have no product links.",
                order.Number);
            return null;
        }

        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(x => x.Id);

        decimal totalWeightGrams = 0;
        decimal maxWidth = 0;
        decimal maxLength = 0;
        decimal maxHeight = 0;

        foreach (var item in order.Items.Where(x => x.ProductId.HasValue))
        {
            if (!productsById.TryGetValue(item.ProductId!.Value, out var product)
                || product.Weight is null
                || product.Width is null
                || product.Length is null
                || product.Height is null)
            {
                _logger.LogWarning(
                    "Packed order {OrderNumber} cannot create SHIPPOP booking because product dimensions are missing. ProductId={ProductId}",
                    order.Number,
                    item.ProductId);
                return null;
            }

            totalWeightGrams += product.Weight.Value * item.Quantity;
            maxWidth = Math.Max(maxWidth, product.Width.Value);
            maxLength = Math.Max(maxLength, product.Length.Value);
            maxHeight = Math.Max(maxHeight, product.Height.Value);
        }

        return new ShippingBookingRequest(
            order.Id,
            order.Number,
            order.ShippingName ?? order.CustomerName ?? "Customer",
            order.ShippingPhone ?? order.CustomerPhone ?? string.Empty,
            order.CustomerEmail,
            parsedAddress.Address,
            parsedAddress.District,
            parsedAddress.State,
            parsedAddress.Province,
            parsedAddress.Postcode,
            order.Number,
            (double)(totalWeightGrams / 1000m),
            (double)maxWidth,
            (double)maxLength,
            (double)maxHeight,
            order.ShippingChannel,
            order.Description,
            order.IsCod ? order.Amount : 0);
    }

    private static bool IsPacked(Order order)
        => string.Equals(order.Status, "Packed", StringComparison.OrdinalIgnoreCase)
           || string.Equals(order.Status, ((int)ZortOrderStatus.Packed).ToString(), StringComparison.OrdinalIgnoreCase);

    private static bool IsDelivered(string status)
        => string.Equals(status, ZortOrderStatus.Success.ToString(), StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, ((int)ZortOrderStatus.Success).ToString(), StringComparison.OrdinalIgnoreCase);

    private static bool IsPaid(Order order)
        => string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(order.PaymentStatus, ((int)ZortPaymentStatus.Paid).ToString(), StringComparison.OrdinalIgnoreCase);

    private static bool IsVoided(string status)
        => string.Equals(status, ZortOrderStatus.Voided.ToString(), StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, ((int)ZortOrderStatus.Voided).ToString(), StringComparison.OrdinalIgnoreCase);

    private static ParsedShippingAddress? ParseShippingAddress(string? shippingAddress)
    {
        var parts = (shippingAddress ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 5)
        {
            return null;
        }

        var postcode = parts[^1];
        var province = parts[^2];
        var state = parts[^3];
        var district = parts[^4];
        var address = BuildSpaceSeparated(parts[..^4]);

        return string.IsNullOrWhiteSpace(address)
            ? null
            : new ParsedShippingAddress(address, district, state, province, postcode);
    }

    private static string BuildSpaceSeparated(IEnumerable<string> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(part.Trim());
        }

        return builder.ToString();
    }

    private sealed record ParsedShippingAddress(
        string Address,
        string District,
        string State,
        string Province,
        string Postcode);

    private static LineOrderNotification CreateLineNotification(
        Order order,
        string status,
        string? trackingNumber)
        => new(
            order.Id,
            order.Number,
            order.IntegrationCustomerId,
            order.CustomerName,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            Status: status,
            TrackingNumber: trackingNumber);

    private static ShopRealtimeNotification CreateShopNotification(
        Order order,
        string type,
        string title,
        string message,
        string status,
        string? trackingNumber = null)
        => new(
            order.CustomerId,
            order.IntegrationCustomerId,
            type,
            order.Id,
            order.Number,
            title,
            message,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            status,
            trackingNumber);
}
