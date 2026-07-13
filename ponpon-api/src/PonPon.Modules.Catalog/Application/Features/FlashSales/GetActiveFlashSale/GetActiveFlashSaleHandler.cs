using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.GetActiveFlashSale;

public sealed class GetActiveFlashSaleHandler
{
    private readonly IFlashSaleRepository _flashSales;
    private readonly IProductRepository _products;
    private readonly IDateTimeProvider _clock;

    public GetActiveFlashSaleHandler(IFlashSaleRepository flashSales, IProductRepository products, IDateTimeProvider clock)
    {
        _flashSales = flashSales;
        _products = products;
        _clock = clock;
    }

    public async Task<FlashSaleResponse?> HandleAsync(CancellationToken cancellationToken = default)
    {
        var localNow = GetFlashSalesHandler.GetBangkokNow(_clock.UtcNow);
        var activeFlashSales = (await _flashSales.GetAllAsync(cancellationToken))
            .Where(x => GetFlashSalesHandler.IsActiveNow(x, localNow))
            .OrderByDescending(x => x.StartDate)
            .ToArray();

        if (activeFlashSales.Length == 0)
            return null;

        var productIds = activeFlashSales.SelectMany(x => x.Products).Select(x => x.ProductId).ToHashSet();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var visibleProductMap = products
            .Where(x => x.IsVisibleToCustomer)
            .ToDictionary(x => x.Id);
        var visibleProductIds = visibleProductMap.Keys.ToHashSet();

        foreach (var flashSale in activeFlashSales)
        {
            var response = GetFlashSalesHandler.MapToResponse(
                flashSale,
                localNow,
                visibleProductMap,
                visibleProductIds);
            if (response.Products.Count > 0)
                return response;
        }

        return null;
    }
}
