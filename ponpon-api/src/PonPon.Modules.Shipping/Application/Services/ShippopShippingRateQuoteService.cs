using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Shipping.Application.Services;

public sealed class ShippopShippingRateQuoteService : IShippingRateQuoteService
{
    private readonly IShippopClient _shippop;

    public ShippopShippingRateQuoteService(IShippopClient shippop)
    {
        _shippop = shippop;
    }

    public async Task<decimal> GetShippingAmountAsync(
        ShippingRateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var rates = await _shippop.CheckRatesAsync(
            new ShippopAddress(
                request.RecipientName,
                request.Address,
                request.District,
                request.State,
                request.Province,
                request.Postcode,
                request.RecipientPhone,
                request.RecipientEmail ?? string.Empty),
            new ShippopParcel(
                request.ParcelName,
                request.WeightKg,
                request.WidthCm,
                request.LengthCm,
                request.HeightCm),
            cancellationToken);

        var matchingRates = rates
            .Where(x =>
                string.Equals(x.CourierCode, request.ShippingChannel, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.ServiceCode, request.ShippingChannel, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingRates.Length == 0)
        {
            throw new BadRequestException(
                $"Shipping channel '{request.ShippingChannel}' is unavailable for this order.");
        }

        return matchingRates.Min(x => x.Price);
    }
}
