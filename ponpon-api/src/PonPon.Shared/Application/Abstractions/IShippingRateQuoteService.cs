namespace PonPon.Shared.Application.Abstractions;

public sealed record ShippingRateQuoteRequest(
    string RecipientName,
    string RecipientPhone,
    string? RecipientEmail,
    string Address,
    string District,
    string State,
    string Province,
    string Postcode,
    string ParcelName,
    double WeightKg,
    double WidthCm,
    double LengthCm,
    double HeightCm,
    string ShippingChannel);

public interface IShippingRateQuoteService
{
    Task<decimal> GetShippingAmountAsync(
        ShippingRateQuoteRequest request,
        CancellationToken cancellationToken = default);
}
