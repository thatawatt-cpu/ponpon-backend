namespace PonPon.Shared.Application.Abstractions;

public interface IShippingBookingAutomation
{
    Task<ShippingBookingResult?> CreateBookingForPackedOrderAsync(
        ShippingBookingRequest request,
        CancellationToken cancellationToken = default);

    Task CancelBookingForOrderAsync(
        Guid orderId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<bool> HasShipmentForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

public sealed record ShippingBookingRequest(
    Guid OrderId,
    string OrderNumber,
    string ToName,
    string ToPhone,
    string? ToEmail,
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
    string? Remark,
    decimal Cod);

public sealed record ShippingBookingResult(
    string TrackingCode,
    string? CourierTrackingCode,
    string CourierCode);
