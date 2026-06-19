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
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var flashSale = await _flashSales.GetActiveAsync(today, cancellationToken);

        if (flashSale is null)
            return null;

        var productIds = flashSale.Products.Select(x => x.ProductId).ToHashSet();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(x => x.Id);

        return GetFlashSalesHandler.MapToResponse(flashSale, today, productMap);
    }
}
