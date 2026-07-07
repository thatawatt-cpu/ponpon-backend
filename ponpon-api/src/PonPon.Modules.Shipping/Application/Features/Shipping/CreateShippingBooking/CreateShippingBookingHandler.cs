using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Domain;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;

namespace PonPon.Modules.Shipping.Application.Features.Shipping.CreateShippingBooking;

public sealed class CreateShippingBookingHandler
{
    private readonly IShippopClient _shippop;
    private readonly IShippingShipmentRepository _shipments;
    private readonly IShippingUnitOfWork _unitOfWork;

    public CreateShippingBookingHandler(
        IShippopClient shippop,
        IShippingShipmentRepository shipments,
        IShippingUnitOfWork unitOfWork)
    {
        _shippop = shippop;
        _shipments = shipments;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateShippingBookingResponse> HandleAsync(
        CreateShippingBookingCommand command,
        CancellationToken cancellationToken)
    {
        var to = new ShippopAddress(
            command.ToName, command.ToAddress,
            command.ToDistrict, command.ToState,
            command.ToProvince, command.ToPostcode,
            command.ToPhone, command.ToEmail);

        var parcel = new ShippopParcel(
            command.ParcelName,
            command.WeightKg, command.WidthCm,
            command.LengthCm, command.HeightCm);

        var booking = await _shippop.CreateBookingAsync(
            to, parcel,
            command.CourierCode, command.ServiceCode,
            command.Remark, command.Cod,
            cancellationToken);

        var now = DateTime.UtcNow;
        var (shipment, _) = await _shipments.GetOrCreateByShippopTrackingCodeAsync(
            booking.TrackingCode,
            "booking",
            now,
            cancellationToken);

        shipment.UpdateFromBooking(
            command.OrderId,
            command.OrderNumber,
            booking.CourierTrackingCode,
            booking.CourierCode,
            courierName: null,
            booking.Price,
            booking.LabelUrl,
            booking.RawJson,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateShippingBookingResponse(
            booking.TrackingCode,
            booking.CourierTrackingCode,
            booking.CourierCode,
            booking.Price,
            booking.LabelUrl);
    }
}
