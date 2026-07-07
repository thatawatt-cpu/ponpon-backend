using System.Globalization;
using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Pricing;

public sealed class FlashSalePricingStep : IPricingStep
{
    private readonly IFlashSaleRepository _flashSales;

    public FlashSalePricingStep(IFlashSaleRepository flashSales)
    {
        _flashSales = flashSales;
    }

    public int Order => 100;

    public async Task ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
    {
        var bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(context.NowUtc, DateTimeKind.Utc),
            bangkok);
        var today = DateOnly.FromDateTime(localNow);
        var nowTime = TimeOnly.FromDateTime(localNow);

        var flashSale = (await _flashSales.GetAllAsync(cancellationToken))
            .Where(x => x.StartDate <= today && today <= x.EndDate)
            .Where(x => IsWithinSlot(x.Slots, nowTime))
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefault();
        if (flashSale is null)
            return;

        var salePrices = flashSale.Products.ToDictionary(x => x.ProductId, x => x.SalePrice);
        foreach (var line in context.Lines)
        {
            if (!salePrices.TryGetValue(line.Input.ProductId, out var salePrice)
                || salePrice < 0
                || salePrice >= line.UnitPrice)
            {
                continue;
            }

            var discount = (line.UnitPrice - salePrice) * line.Input.Quantity;
            line.UnitPrice = salePrice;
            context.AppliedFlashSaleId = flashSale.Id;
            context.Adjustments.Add(new PriceAdjustment(
                "flash_sale",
                flashSale.Id.ToString(),
                flashSale.Name,
                discount,
                line.Input.ProductId));
        }
    }

    private static bool IsWithinSlot(IReadOnlyCollection<string> slots, TimeOnly now)
    {
        if (slots.Count == 0)
            return true;

        return slots.Any(slot =>
        {
            var parts = slot.Split('-', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2
                && TimeOnly.TryParse(parts[0], CultureInfo.InvariantCulture, out var start)
                && TimeOnly.TryParse(parts[1], CultureInfo.InvariantCulture, out var end)
                && (start <= end
                    ? start <= now && now <= end
                    : now >= start || now <= end);
        });
    }
}
