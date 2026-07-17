namespace PonPon.Modules.Shipping.Application.Features.Shipping.CheckShippingRates;

public sealed record CheckShippingRatesRequest(
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
    double HeightCm);

public sealed record CheckShippingRatesQuery(
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
    double HeightCm);

public sealed record ShippingRateResponse(
    string CourierCode,
    string CourierName,
    string ServiceName,
    string ServiceCode,
    decimal Price,
    string? EstimateTime);
