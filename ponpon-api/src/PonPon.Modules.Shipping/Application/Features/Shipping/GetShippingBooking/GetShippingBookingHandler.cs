using PonPon.Modules.Shipping.Application.Abstractions;

namespace PonPon.Modules.Shipping.Application.Features.Shipping.GetShippingBooking;

public sealed class GetShippingBookingHandler
{
    private readonly IShippopClient _shippop;

    public GetShippingBookingHandler(IShippopClient shippop) => _shippop = shippop;

    public async Task<GetShippingBookingResponse> HandleAsync(
        GetShippingBookingQuery query,
        CancellationToken cancellationToken)
    {
        var booking = await _shippop.GetBookingAsync(query.TrackingCode, cancellationToken);

        return new GetShippingBookingResponse(
            booking.TrackingCode,
            booking.CourierTrackingCode,
            booking.CourierCode,
            booking.CourierName,
            booking.BookingStatus,
            booking.Price,
            booking.LabelUrl);
    }
}
