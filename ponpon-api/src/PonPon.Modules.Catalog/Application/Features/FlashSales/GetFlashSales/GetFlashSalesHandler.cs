using System.Globalization;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;

public sealed class GetFlashSalesHandler
{
    private const string BangkokTimeZoneId = "Asia/Bangkok";
    private readonly IFlashSaleRepository _flashSales;
    private readonly IProductRepository _products;
    private readonly IDateTimeProvider _clock;

    public GetFlashSalesHandler(IFlashSaleRepository flashSales, IProductRepository products, IDateTimeProvider clock)
    {
        _flashSales = flashSales;
        _products = products;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<FlashSaleResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var flashSales = await _flashSales.GetAllAsync(cancellationToken);
        var productIds = flashSales.SelectMany(x => x.Products).Select(x => x.ProductId).ToHashSet();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(x => x.Id);

        var localNow = GetBangkokNow(_clock.UtcNow);
        return flashSales.Select(fs => MapToResponse(fs, localNow, productMap)).ToArray();
    }

    internal static FlashSaleResponse MapToResponse(
        Domain.FlashSales.FlashSale fs,
        DateTime localNow,
        Dictionary<Guid, Domain.Products.Product> productMap,
        IReadOnlySet<Guid>? includedProductIds = null)
    {
        var today = DateOnly.FromDateTime(localNow);
        var nowTime = TimeOnly.FromDateTime(localNow);
        var status = !fs.IsActive
            ? "inactive"
            : today < fs.StartDate
            ? "upcoming"
            : today > fs.EndDate
                ? "ended"
                : IsWithinSlot(fs.Slots, nowTime) ? "active" : "inactive";
        var products = fs.Products
            .Where(p => includedProductIds is null || includedProductIds.Contains(p.ProductId))
            .Select(p =>
        {
            productMap.TryGetValue(p.ProductId, out var product);
            return new FlashSaleProductResponse(
                p.ProductId,
                p.SalePrice,
                p.QuantityLimit,
                p.ReservedQuantity,
                product?.Name ?? string.Empty,
                product?.OriginalPrice,
                product?.ImageUrl);
        }).ToArray();

        return new FlashSaleResponse(
            fs.Id,
            fs.Name,
            fs.StartDate,
            fs.EndDate,
            fs.IsActive,
            fs.Slots,
            status,
            products);
    }

    internal static DateTime GetBangkokNow(DateTime utcNow)
    {
        var bangkok = TimeZoneInfo.FindSystemTimeZoneById(BangkokTimeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), bangkok);
    }

    internal static bool IsActiveNow(Domain.FlashSales.FlashSale fs, DateTime localNow)
    {
        var today = DateOnly.FromDateTime(localNow);
        return fs.StartDate <= today
            && today <= fs.EndDate
            && fs.IsActive
            && IsWithinSlot(fs.Slots, TimeOnly.FromDateTime(localNow));
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
