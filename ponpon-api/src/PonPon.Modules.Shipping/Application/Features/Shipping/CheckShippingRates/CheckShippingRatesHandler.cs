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

        var cheapest = valueRates
            .OrderBy(x => x.Rate.Price)
            .ThenBy(x => x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .First();

        var fastest = valueRates
            .Where(x => x.Estimate.MinDays.HasValue)
            .OrderBy(x => x.Estimate.MinDays)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? cheapest;

        var standard = SelectStandardByDeliveryDays(valueRates, [cheapest, fastest]);

        var options = new List<ShippingRateResponse>
        {
            ToResponse(cheapest, "cheapest", "ถูกสุด", standard is null)
        };

        if (standard is not null)
        {
            options.Add(ToResponse(standard, "standard", "เวลากลางๆ", true));
        }

        if (!options.Any(x => SameShippingOption(x, fastest.Rate)))
        {
            options.Add(ToResponse(fastest, "fastest", "เร็วสุด", false));
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

    private static RateCandidate? SelectStandardByDeliveryDays(
        IReadOnlyCollection<RateCandidate> rates,
        IReadOnlyCollection<RateCandidate> excludedRates)
    {
        var ordered = rates
            .Where(x => !excludedRates.Any(excluded =>
                SameShippingOption(excluded.Rate, x.Rate)
                || SameDeliveryWindow(excluded.Estimate, x.Estimate)))
            .OrderBy(x => x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ordered.Length == 0)
            return null;

        return ordered[(ordered.Length - 1) / 2];
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

    private static bool SameDeliveryWindow(ParsedEstimate left, ParsedEstimate right)
        => left.MinDays == right.MinDays && left.MaxDays == right.MaxDays;

    private static bool IsNoMoreExpensive(RateCandidate left, RateCandidate right)
        => left.Rate.Price <= right.Rate.Price;

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
