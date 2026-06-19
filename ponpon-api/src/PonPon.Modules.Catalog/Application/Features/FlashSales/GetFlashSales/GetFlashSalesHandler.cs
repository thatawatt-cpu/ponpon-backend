using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;

public sealed class GetFlashSalesHandler
{
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

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        return flashSales.Select(fs => MapToResponse(fs, today, productMap)).ToArray();
    }

    internal static FlashSaleResponse MapToResponse(Domain.FlashSales.FlashSale fs, DateOnly today, Dictionary<Guid, Domain.Products.Product> productMap)
    {
        var status = today < fs.StartDate ? "upcoming" : today > fs.EndDate ? "ended" : "active";
        var products = fs.Products.Select(p =>
        {
            productMap.TryGetValue(p.ProductId, out var product);
            return new FlashSaleProductResponse(
                p.ProductId,
                p.SalePrice,
                product?.Name ?? string.Empty,
                product?.OriginalPrice,
                product?.ImageUrl);
        }).ToArray();

        return new FlashSaleResponse(fs.Id, fs.Name, fs.StartDate, fs.EndDate, fs.Slots, status, products);
    }
}
