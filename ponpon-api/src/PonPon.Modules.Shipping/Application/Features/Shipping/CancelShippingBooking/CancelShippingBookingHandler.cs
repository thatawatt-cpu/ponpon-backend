using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Domain;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Shipping.Application.Features.Shipping.CancelShippingBooking;

public sealed class CancelShippingBookingHandler
{
    private readonly IShippopClient _shippop;
    private readonly IShippingShipmentRepository _shipments;
    private readonly IShippingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CancelShippingBookingHandler(
        IShippopClient shippop,
        IShippingShipmentRepository shipments,
        IShippingUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _shippop = shippop;
        _shipments = shipments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(CancelShippingBookingCommand command, CancellationToken cancellationToken)
    {
        await _shippop.CancelBookingAsync(command.TrackingCode, cancellationToken);

        var shipment = await _shipments.GetByShippopTrackingCodeAsync(command.TrackingCode, cancellationToken)
            ?? throw new NotFoundException("Shipping shipment was not found.");

        var now = _clock.UtcNow;
        const string payloadJson = "{\"source\":\"admin\"}";
        shipment.MarkCanceled(payloadJson, now);
        shipment.AddEvent(ShippingShipmentEvent.Create(
            shipment.Id,
            shipment.ShippopTrackingCode,
            "canceled",
            shipment.CourierTrackingCode,
            now,
            payloadJson,
            now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
