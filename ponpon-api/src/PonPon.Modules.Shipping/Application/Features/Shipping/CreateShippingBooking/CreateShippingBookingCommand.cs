namespace PonPon.Modules.Shipping.Application.Features.Shipping.CreateShippingBooking;

public sealed record CreateShippingBookingRequest(
    string ToName,
    string ToPhone,
    string ToEmail,
    string ToAddress,
    string ToDistrict,
    string ToState,
    string ToProvince,
    string ToPostcode,
    string ParcelName,
    double WeightKg,
    double WidthCm,
    double LengthCm,
    double HeightCm,
    string CourierCode,
    string ServiceCode,
    string? Remark,
    decimal Cod,
    Guid? OrderId = null,
    string? OrderNumber = null);

public sealed record CreateShippingBookingCommand(
    string ToName,
    string ToPhone,
    string ToEmail,
    string ToAddress,
    string ToDistrict,
    string ToState,
    string ToProvince,
    string ToPostcode,
    string ParcelName,
    double WeightKg,
    double WidthCm,
    double LengthCm,
    double HeightCm,
    string CourierCode,
    string ServiceCode,
    string? Remark,
    decimal Cod,
    Guid? OrderId = null,
    string? OrderNumber = null);

public sealed record CreateShippingBookingResponse(
    string TrackingCode,
    string? CourierTrackingCode,
    string CourierCode,
    decimal Price,
    string? LabelUrl);
