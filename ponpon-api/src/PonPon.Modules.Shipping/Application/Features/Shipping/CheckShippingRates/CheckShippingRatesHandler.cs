using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;

namespace PonPon.Modules.Shipping.Application.Features.Shipping.CheckShippingRates;

public sealed class CheckShippingRatesHandler
{
    private readonly IShippopClient _shippop;

    public CheckShippingRatesHandler(IShippopClient shippop) => _shippop = shippop;

    public async Task<IReadOnlyList<ShippingRateResponse>> HandleAsync(
        CheckShippingRatesQuery query,
        CancellationToken cancellationToken)
    {
        var to = new ShippopAddress(
            query.ToName, query.ToAddress,
            query.ToDistrict, query.ToState,
            query.ToProvince, query.ToPostcode,
            query.ToPhone, query.ToEmail);

        var parcel = new ShippopParcel(
            query.ParcelName,
            query.WeightKg, query.WidthCm,
            query.LengthCm, query.HeightCm);

        var rates = await _shippop.CheckRatesAsync(to, parcel, cancellationToken);

        return rates.Select(r => new ShippingRateResponse(
            r.CourierCode, r.CourierName,
            r.ServiceName, r.ServiceCode,
            r.Price)).ToArray();
    }
}
