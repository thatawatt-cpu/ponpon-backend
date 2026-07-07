namespace PonPon.Modules.Shipping.Application.Features.Shipping.GetShippingBooking;

public sealed record GetShippingBookingQuery(string TrackingCode);

public sealed record GetShippingBookingResponse(
    string TrackingCode,
    string? CourierTrackingCode,
    string CourierCode,
    string? CourierName,
    string Status,
    decimal Price,
    string? LabelUrl);
