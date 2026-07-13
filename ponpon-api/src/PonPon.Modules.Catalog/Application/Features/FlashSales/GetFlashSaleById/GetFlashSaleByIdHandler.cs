using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSaleById;

public sealed class GetFlashSaleByIdHandler
{
    private readonly IFlashSaleRepository _flashSales;
    private readonly IProductRepository _products;
    private readonly IDateTimeProvider _clock;

    public GetFlashSaleByIdHandler(IFlashSaleRepository flashSales, IProductRepository products, IDateTimeProvider clock)
    {
        _flashSales = flashSales;
        _products = products;
        _clock = clock;
    }

    public async Task<FlashSaleResponse> HandleAsync(GetFlashSaleByIdQuery query, CancellationToken cancellationToken = default)
    {
        var flashSale = await _flashSales.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException("Flash sale was not found.");

        var productIds = flashSale.Products.Select(x => x.ProductId).ToHashSet();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(x => x.Id);

        var localNow = GetFlashSalesHandler.GetBangkokNow(_clock.UtcNow);
        return GetFlashSalesHandler.MapToResponse(flashSale, localNow, productMap);
    }
}
