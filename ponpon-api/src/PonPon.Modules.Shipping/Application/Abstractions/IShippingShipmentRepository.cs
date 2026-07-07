using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Application.Abstractions;

public interface IShippingShipmentRepository
{
    Task<ShippingShipment?> GetByShippopTrackingCodeAsync(
        string shippopTrackingCode,
        CancellationToken cancellationToken = default);

    Task<ShippingShipment?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<(ShippingShipment Shipment, bool Created)> GetOrCreateByShippopTrackingCodeAsync(
        string shippopTrackingCode,
        string initialStatus,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddWebhookEventAsync(
        ShippingShipmentEvent shipmentEvent,
        CancellationToken cancellationToken = default);

    Task AddAsync(ShippingShipment shipment, CancellationToken cancellationToken = default);
}
