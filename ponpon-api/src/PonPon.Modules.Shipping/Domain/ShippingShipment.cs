using PonPon.Shared.Domain;

namespace PonPon.Modules.Shipping.Domain;

public sealed class ShippingShipment : AggregateRoot, IAuditableEntity
{
    private readonly List<ShippingShipmentEvent> _events = [];

    private ShippingShipment()
    {
        ShippopTrackingCode = string.Empty;
        ShippingStatus = string.Empty;
    }

    public Guid? OrderId { get; private set; }
    public string? OrderNumber { get; private set; }
    public string ShippopTrackingCode { get; private set; }
    public string? CourierTrackingCode { get; private set; }
    public string? CourierCode { get; private set; }
    public string? CourierName { get; private set; }
    public string ShippingStatus { get; private set; }
    public decimal Price { get; private set; }
    public string? LabelUrl { get; private set; }
    public string? RawBookingJson { get; private set; }
    public string? LastWebhookJson { get; private set; }
    public DateTime? LastStatusAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public IReadOnlyCollection<ShippingShipmentEvent> Events => _events.AsReadOnly();

    public static ShippingShipment CreateFromBooking(
        Guid? orderId,
        string? orderNumber,
        string shippopTrackingCode,
        string? courierTrackingCode,
        string? courierCode,
        string? courierName,
        decimal price,
        string? labelUrl,
        string? rawBookingJson,
        DateTime now)
    {
        var shipment = new ShippingShipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderNumber = EmptyToNull(orderNumber),
            ShippopTrackingCode = shippopTrackingCode.Trim(),
            CourierTrackingCode = EmptyToNull(courierTrackingCode),
            CourierCode = EmptyToNull(courierCode),
            CourierName = EmptyToNull(courierName),
            ShippingStatus = "booking",
            Price = price,
            LabelUrl = EmptyToNull(labelUrl),
            RawBookingJson = EmptyToNull(rawBookingJson),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return shipment;
    }

    public static ShippingShipment CreateFromWebhook(
        string shippopTrackingCode,
        string shippingStatus,
        string? courierTrackingCode,
        string? webhookJson,
        DateTime? statusAtUtc,
        DateTime now)
    {
        var shipment = new ShippingShipment
        {
            Id = Guid.NewGuid(),
            ShippopTrackingCode = shippopTrackingCode.Trim(),
            ShippingStatus = shippingStatus.Trim(),
            CourierTrackingCode = EmptyToNull(courierTrackingCode),
            LastWebhookJson = EmptyToNull(webhookJson),
            LastStatusAtUtc = statusAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return shipment;
    }

    public void UpdateFromBooking(
        Guid? orderId,
        string? orderNumber,
        string? courierTrackingCode,
        string? courierCode,
        string? courierName,
        decimal price,
        string? labelUrl,
        string? rawBookingJson,
        DateTime now)
    {
        OrderId = orderId ?? OrderId;
        OrderNumber = EmptyToNull(orderNumber) ?? OrderNumber;
        CourierTrackingCode = EmptyToNull(courierTrackingCode) ?? CourierTrackingCode;
        CourierCode = EmptyToNull(courierCode) ?? CourierCode;
        CourierName = EmptyToNull(courierName) ?? CourierName;
        Price = price;
        LabelUrl = EmptyToNull(labelUrl) ?? LabelUrl;
        RawBookingJson = EmptyToNull(rawBookingJson) ?? RawBookingJson;
        UpdatedAtUtc = now;
    }

    public bool ApplyWebhook(
        string shippingStatus,
        string? courierTrackingCode,
        string? webhookJson,
        DateTime? statusAtUtc,
        DateTime now)
    {
        var normalizedStatus = shippingStatus.Trim();
        var canApplyStatus = CanApplyWebhookStatus(normalizedStatus, statusAtUtc);

        if (!canApplyStatus)
        {
            CourierTrackingCode = EmptyToNull(courierTrackingCode) ?? CourierTrackingCode;
            UpdatedAtUtc = now;
            return false;
        }

        ShippingStatus = normalizedStatus;
        CourierTrackingCode = EmptyToNull(courierTrackingCode) ?? CourierTrackingCode;
        LastWebhookJson = EmptyToNull(webhookJson);
        LastStatusAtUtc = statusAtUtc ?? LastStatusAtUtc;
        UpdatedAtUtc = now;
        return true;
    }

    public void MarkCanceled(string? rawPayloadJson, DateTime now)
    {
        ShippingStatus = "canceled";
        LastWebhookJson = EmptyToNull(rawPayloadJson) ?? LastWebhookJson;
        LastStatusAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void AddEvent(ShippingShipmentEvent shipmentEvent)
    {
        _events.Add(shipmentEvent);
    }

    public bool HasWebhookEvent(string webhookEventKey)
    {
        return _events.Any(x => string.Equals(
            x.WebhookEventKey,
            webhookEventKey,
            StringComparison.OrdinalIgnoreCase));
    }

    private bool CanApplyWebhookStatus(string incomingStatus, DateTime? statusAtUtc)
    {
        if (statusAtUtc.HasValue
            && LastStatusAtUtc.HasValue
            && statusAtUtc.Value < LastStatusAtUtc.Value)
        {
            return false;
        }

        if (string.Equals(ShippingStatus, incomingStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var incomingRank = GetProgressRank(incomingStatus);
        if (incomingRank is null)
        {
            return false;
        }

        var currentRank = GetProgressRank(ShippingStatus);
        return currentRank is null || incomingRank.Value >= currentRank.Value;
    }

    private static int? GetProgressRank(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "wait" or "unpaid" => 0,
            "booking" or "cancel" or "invalid" or "rider_accept" => 1,
            "shipping" or "problem" => 2,
            "return_shipping" or "return_problem" => 3,
            "complete" or "completed" or "success" or "successful" or "delivered" or "delivered_success" or
                "delivery_success" or "finish" or "finished" or "return" or "return_complete" or "return_return" or
                "return_close" => 4,
            "close" or "canceled" => 5,
            "package_detail" or "pending_transfer" or "transferred" => null,
            _ => 2
        };
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
