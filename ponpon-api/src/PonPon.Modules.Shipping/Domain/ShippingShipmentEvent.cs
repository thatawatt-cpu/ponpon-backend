using PonPon.Shared.Domain;

namespace PonPon.Modules.Shipping.Domain;

public sealed class ShippingShipmentEvent : Entity
{
    private ShippingShipmentEvent()
    {
        ShippopTrackingCode = string.Empty;
        OrderStatus = string.Empty;
        RawPayloadJson = "{}";
    }

    public Guid ShipmentId { get; private set; }
    public string ShippopTrackingCode { get; private set; }
    public string OrderStatus { get; private set; }
    public string? CourierTrackingCode { get; private set; }
    public DateTime? EventDateTimeUtc { get; private set; }
    public string? WebhookEventKey { get; private set; }
    public string RawPayloadJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ShippingShipmentEvent Create(
        Guid shipmentId,
        string shippopTrackingCode,
        string orderStatus,
        string? courierTrackingCode,
        DateTime? eventDateTimeUtc,
        string rawPayloadJson,
        DateTime now,
        string? webhookEventKey = null)
    {
        return new ShippingShipmentEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            ShippopTrackingCode = shippopTrackingCode.Trim(),
            OrderStatus = orderStatus.Trim(),
            CourierTrackingCode = string.IsNullOrWhiteSpace(courierTrackingCode) ? null : courierTrackingCode.Trim(),
            EventDateTimeUtc = eventDateTimeUtc,
            WebhookEventKey = string.IsNullOrWhiteSpace(webhookEventKey) ? null : webhookEventKey.Trim(),
            RawPayloadJson = string.IsNullOrWhiteSpace(rawPayloadJson) ? "{}" : rawPayloadJson,
            CreatedAtUtc = now
        };
    }
}
