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

        var standard = availableRates
            .OrderBy(x => x.Rate.Price)
            .ThenBy(x => x.Estimate.MinDays ?? int.MaxValue)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .First();

        var fastest = availableRates
            .Where(x => x.Estimate.MinDays.HasValue)
            .OrderBy(x => x.Estimate.MinDays)
            .ThenBy(x => x.Estimate.MaxDays ?? x.Estimate.MinDays)
            .ThenBy(x => x.Rate.Price)
            .ThenBy(x => x.Rate.CourierCode, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (fastest is null || SameShippingOption(standard.Rate, fastest.Rate))
        {
            return [ToResponse(standard, "standard_fastest", "ปานกลางและเร็วที่สุด", true)];
        }

        return
        [
            ToResponse(standard, "standard", "ปานกลาง", true),
            ToResponse(fastest, "fastest", "เร็วที่สุด", false)
        ];
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

    private static bool SameShippingOption(ShippopRateDto left, ShippopRateDto right)
        => string.Equals(left.CourierCode, right.CourierCode, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.ServiceCode, right.ServiceCode, StringComparison.OrdinalIgnoreCase);

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
