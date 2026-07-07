using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Domain;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence.Repositories;

public sealed class ShippingShipmentRepository : IShippingShipmentRepository
{
    private readonly ShippingDbContext _dbContext;

    public ShippingShipmentRepository(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ShippingShipment?> GetByShippopTrackingCodeAsync(
        string shippopTrackingCode,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.ShippopTrackingCode == shippopTrackingCode, cancellationToken);
    }

    public Task<ShippingShipment?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Shipments
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task<(ShippingShipment Shipment, bool Created)> GetOrCreateByShippopTrackingCodeAsync(
        string shippopTrackingCode,
        string initialStatus,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var trackingCode = shippopTrackingCode.Trim();
        var status = initialStatus.Trim();
        var id = Guid.NewGuid();

        var created = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO shipping.shipping_shipments
                 ("Id", "ShippopTrackingCode", "ShippingStatus", "Price", "CreatedAtUtc", "UpdatedAtUtc")
             VALUES
                 ({id}, {trackingCode}, {status}, {0m}, {now}, {now})
             ON CONFLICT ("ShippopTrackingCode") DO NOTHING
             """,
            cancellationToken) > 0;

        var shipment = await GetByShippopTrackingCodeAsync(trackingCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"SHIPPOP shipment {trackingCode} could not be loaded after creation.");

        return (shipment, created);
    }

    public async Task<bool> TryAddWebhookEventAsync(
        ShippingShipmentEvent shipmentEvent,
        CancellationToken cancellationToken = default)
    {
        var inserted = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO shipping.shipping_shipment_events
                 ("Id", "ShipmentId", "ShippopTrackingCode", "OrderStatus",
                  "CourierTrackingCode", "EventDateTimeUtc", "WebhookEventKey",
                  "RawPayloadJson", "CreatedAtUtc")
             VALUES
                 ({shipmentEvent.Id}, {shipmentEvent.ShipmentId}, {shipmentEvent.ShippopTrackingCode},
                  {shipmentEvent.OrderStatus}, {shipmentEvent.CourierTrackingCode},
                  {shipmentEvent.EventDateTimeUtc}, {shipmentEvent.WebhookEventKey},
                  CAST({shipmentEvent.RawPayloadJson} AS jsonb), {shipmentEvent.CreatedAtUtc})
             ON CONFLICT DO NOTHING
             """,
            cancellationToken);

        return inserted > 0;
    }

    public async Task AddAsync(ShippingShipment shipment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Shipments.AddAsync(shipment, cancellationToken);
    }
}
