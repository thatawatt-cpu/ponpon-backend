using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler
{
    private readonly IProductRepository _products;
    private readonly PonPon.Shared.Application.Abstractions.IProductSalesReadService _sales;
    private readonly ProductDetailPriceResolver _priceResolver;

    public GetProductsHandler(
        IProductRepository products,
        PonPon.Shared.Application.Abstractions.IProductSalesReadService sales,
        ProductDetailPriceResolver priceResolver)
    {
        _products = products;
        _sales = sales;
        _priceResolver = priceResolver;
    }

    public async Task<IReadOnlyCollection<ProductListItemResponse>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var products = query.IncludeInactive
            ? await _products.GetAdminProductListItemsAsync(query.Keyword, query.Status, query.Source, page, pageSize, cancellationToken)
            : await _products.GetCustomerProductListItemsAsync(query.Keyword, query.Category, page, pageSize, cancellationToken);
        var soldCounts = await _sales.GetSoldCountsAsync(products.Select(x => x.Id).ToArray(), cancellationToken);
        var prices = await _priceResolver.ResolveAsync(products, cancellationToken);

        return products.Select(x => ProductListItemResponseFactory.Create(x, soldCounts, prices)).ToArray();
    }
}
