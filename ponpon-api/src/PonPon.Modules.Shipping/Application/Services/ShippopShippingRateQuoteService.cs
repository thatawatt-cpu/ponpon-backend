using Microsoft.Extensions.Caching.Memory;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Shipping.Application.Services;

public sealed class ShippopShippingRateQuoteService : IShippingRateQuoteService
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(1),
        Size = 1
    };

    private readonly IShippopClient _shippop;
    private readonly IMemoryCache _cache;

    public ShippopShippingRateQuoteService(IShippopClient shippop, IMemoryCache cache)
    {
        _shippop = shippop;
        _cache = cache;
    }

    public async Task<decimal> GetShippingAmountAsync(
        ShippingRateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(request);
        if (_cache.TryGetValue(cacheKey, out decimal cachedAmount))
            return cachedAmount;

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

        var amount = matchingRates.Min(x => x.Price);
        _cache.Set(cacheKey, amount, CacheOptions);
        return amount;
    }

    private static string CacheKey(ShippingRateQuoteRequest request)
        => string.Join('|',
            "shippop-rate",
            Normalize(request.Address),
            Normalize(request.District),
            Normalize(request.State),
            Normalize(request.Province),
            Normalize(request.Postcode),
            Math.Round(request.WeightKg, 3, MidpointRounding.AwayFromZero),
            Math.Round(request.WidthCm, 1, MidpointRounding.AwayFromZero),
            Math.Round(request.LengthCm, 1, MidpointRounding.AwayFromZero),
            Math.Round(request.HeightCm, 1, MidpointRounding.AwayFromZero),
            Normalize(request.ShippingChannel));

    private static string Normalize(string value)
        => value.Trim().ToLowerInvariant();
}
