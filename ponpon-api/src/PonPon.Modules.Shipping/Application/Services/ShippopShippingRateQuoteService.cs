using Microsoft.Extensions.Caching.Memory;
using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using System.Text.RegularExpressions;

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
        var matchingRates = (await GetShippingOptionsAsync(request, cancellationToken))
            .Where(x => string.Equals(x.ShippingChannel, request.ShippingChannel, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingRates.Length == 0)
        {
            throw new BadRequestException(
                $"Shipping channel '{request.ShippingChannel}' is unavailable for this order.");
        }

        return matchingRates.Min(x => x.Amount);
    }

    public async Task<IReadOnlyCollection<ShippingRateQuoteOption>> GetShippingOptionsAsync(
        ShippingRateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(request, includeShippingChannel: false);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyCollection<ShippingRateQuoteOption>? cachedOptions)
            && cachedOptions is not null)
        {
            return cachedOptions;
        }

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

        var options = rates
            .Where(x => !string.IsNullOrWhiteSpace(x.CourierCode))
            .GroupBy(x => x.CourierCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x =>
            {
                var estimates = x
                    .Select(rate => ParseEstimateDays(rate.EstimateTime))
                    .Where(estimate => estimate.MinDays.HasValue)
                    .ToArray();
                return new ShippingRateQuoteOption(
                    x.Key,
                    x.Min(rate => rate.Price),
                    estimates.Length == 0 ? null : estimates.Min(estimate => estimate.MinDays),
                    estimates.Length == 0 ? null : estimates.Min(estimate => estimate.MaxDays ?? estimate.MinDays));
            })
            .ToArray();
        _cache.Set(cacheKey, options, CacheOptions);
        return options;
    }

    private static string CacheKey(ShippingRateQuoteRequest request, bool includeShippingChannel)
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
            includeShippingChannel ? Normalize(request.ShippingChannel) : "-");

    private static string Normalize(string value)
        => value.Trim().ToLowerInvariant();

    private static ParsedEstimate ParseEstimateDays(string? estimateTime)
    {
        if (string.IsNullOrWhiteSpace(estimateTime))
            return new ParsedEstimate(null, null);

        var values = Regex.Matches(estimateTime, @"\d+")
            .Select(x => int.TryParse(x.Value, out var value) ? value : (int?)null)
            .OfType<int>()
            .ToArray();

        if (values.Length == 0)
            return new ParsedEstimate(null, null);

        return new ParsedEstimate(values.Min(), values.Max());
    }

    private sealed record ParsedEstimate(int? MinDays, int? MaxDays);
}
