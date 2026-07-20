using PonPon.Modules.Shipping.Application.Abstractions;
using PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;
using System.Text.RegularExpressions;

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

        return BuildCustomerOptions(rates);
    }

    private static IReadOnlyList<ShippingRateResponse> BuildCustomerOptions(
        IReadOnlyList<ShippopRateDto> rates)
    {
        var availableRates = rates
            .Where(x => !string.IsNullOrWhiteSpace(x.CourierCode))
            .Select(x => new RateCandidate(x, ParseEstimateDays(x.EstimateTime)))
            .ToArray();

        if (availableRates.Length == 0)
            return [];

        var valueRates = RemoveDominatedRates(availableRates);

        var cheapest = availableRates
            .OrderBy(x => x.Rate.Price)
            .ThenBy(x => x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .First();

        var fastest = availableRates
            .Where(x => x.Estimate.MinDays.HasValue)
            .OrderBy(x => x.Estimate.MinDays)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays)
            .ThenBy(x => x.Rate.Price)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? cheapest;

        var bestValue = SelectBestValue(valueRates, fastest) ?? fastest;

        var options = new List<ShippingRateResponse>
        {
            ToResponse(bestValue, "best_value", "คุ้มที่สุด", true)
        };

        if (!SameShippingOption(bestValue.Rate, fastest.Rate))
        {
            options.Add(ToResponse(fastest, "fastest", "เร็วที่สุด", false));
        }

        return options;
    }

    private static ShippingRateResponse ToResponse(
        RateCandidate candidate,
        string optionType,
        string label,
        bool isDefault)
        => new(
            candidate.Rate.CourierCode,
            candidate.Rate.CourierName,
            candidate.Rate.ServiceName,
            candidate.Rate.ServiceCode,
            candidate.Rate.Price,
            candidate.Rate.EstimateTime,
            optionType,
            label,
            isDefault,
            candidate.Estimate.MinDays,
            candidate.Estimate.MaxDays);

    private static RateCandidate? SelectBestValue(
        IReadOnlyCollection<RateCandidate> rates,
        RateCandidate fastest)
    {
        var candidates = rates
            .Where(x => !SameShippingOption(x.Rate, fastest.Rate))
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var minPrice = candidates.Min(x => x.Rate.Price);
        var maxPrice = candidates.Max(x => x.Rate.Price);
        var minDays = candidates.Min(GetComparableDeliveryDays);
        var maxDays = candidates.Max(GetComparableDeliveryDays);

        return candidates
            .OrderBy(x => GetNormalizedValueScore(x, minPrice, maxPrice, minDays, maxDays))
            .ThenBy(x => x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Rate.Price)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static IReadOnlyCollection<RateCandidate> RemoveDominatedRates(IReadOnlyCollection<RateCandidate> rates)
    {
        var valueRates = rates
            .Where(candidate => !rates.Any(other =>
                !SameShippingOption(candidate.Rate, other.Rate)
                && IsNoMoreExpensive(other, candidate)
                && IsNoSlower(other, candidate)
                && (other.Rate.Price < candidate.Rate.Price || IsStrictlyFaster(other, candidate))))
            .ToArray();

        return valueRates.Length == 0 ? rates : valueRates;
    }

    private static bool SameShippingOption(ShippingRateResponse left, ShippopRateDto right)
        => string.Equals(left.CourierCode, right.CourierCode, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.ServiceCode, right.ServiceCode, StringComparison.OrdinalIgnoreCase);

    private static bool SameShippingOption(ShippopRateDto left, ShippopRateDto right)
        => string.Equals(left.CourierCode, right.CourierCode, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.ServiceCode, right.ServiceCode, StringComparison.OrdinalIgnoreCase);

    private static bool IsNoMoreExpensive(RateCandidate left, RateCandidate right)
        => left.Rate.Price <= right.Rate.Price;

    private static decimal GetNormalizedValueScore(
        RateCandidate candidate,
        decimal minPrice,
        decimal maxPrice,
        int minDays,
        int maxDays)
    {
        var priceRange = maxPrice - minPrice;
        var priceScore = priceRange <= 0
            ? 0m
            : (candidate.Rate.Price - minPrice) / priceRange;
        var dayRange = maxDays - minDays;
        var deliveryScore = dayRange <= 0
            ? 0m
            : (GetComparableDeliveryDays(candidate) - minDays) / (decimal)dayRange;

        return (priceScore + deliveryScore) / 2m;
    }

    private static int GetComparableDeliveryDays(RateCandidate candidate)
        => candidate.Estimate.MaxDays
           ?? candidate.Estimate.MinDays
           ?? int.MaxValue;

    private static bool IsNoSlower(RateCandidate left, RateCandidate right)
    {
        if (!left.Estimate.MinDays.HasValue)
            return false;
        if (!right.Estimate.MinDays.HasValue)
            return true;

        var leftMax = left.Estimate.MaxDays ?? left.Estimate.MinDays.Value;
        var rightMax = right.Estimate.MaxDays ?? right.Estimate.MinDays.Value;
        return left.Estimate.MinDays.Value <= right.Estimate.MinDays.Value && leftMax <= rightMax;
    }

    private static bool IsStrictlyFaster(RateCandidate left, RateCandidate right)
    {
        if (!left.Estimate.MinDays.HasValue)
            return false;
        if (!right.Estimate.MinDays.HasValue)
            return true;

        var leftMax = left.Estimate.MaxDays ?? left.Estimate.MinDays.Value;
        var rightMax = right.Estimate.MaxDays ?? right.Estimate.MinDays.Value;
        return left.Estimate.MinDays.Value < right.Estimate.MinDays.Value || leftMax < rightMax;
    }

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

    private sealed record RateCandidate(ShippopRateDto Rate, ParsedEstimate Estimate);
}
