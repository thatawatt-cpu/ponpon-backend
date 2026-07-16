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
        var (soldCounts, prices) = await EnrichAsync(products, cancellationToken);

        return products.Select(x => ProductListItemResponseFactory.Create(x, soldCounts, prices)).ToArray();
    }

    public async Task<ProductListPageResponse> HandleAdminPageAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var result = await _products.GetAdminProductListPageAsync(
            query.Keyword,
            query.Category,
            query.Status,
            query.Source,
            page,
            pageSize,
            cancellationToken);
        var (soldCounts, prices) = await EnrichAsync(result.Items, cancellationToken);
        var items = result.Items
            .Select(x => ProductListItemResponseFactory.Create(x, soldCounts, prices))
            .ToArray();

        return new ProductListPageResponse(
            items,
            result.Total,
            page,
            pageSize,
            Math.Max(1, (int)Math.Ceiling(result.Total / (double)pageSize)));
    }

    private async Task<(
        IReadOnlyDictionary<Guid, int> SoldCounts,
        IReadOnlyDictionary<Guid, ProductDetailPrice> Prices)> EnrichAsync(
        IReadOnlyCollection<ProductListItemReadModel> products,
        CancellationToken cancellationToken)
    {
        var productIds = products.Select(x => x.Id).ToArray();
        var soldCountsTask = _sales.GetSoldCountsAsync(productIds, cancellationToken);
        var pricesTask = _priceResolver.ResolveAsync(products, cancellationToken);
        await Task.WhenAll(soldCountsTask, pricesTask);
        return (await soldCountsTask, await pricesTask);
    }
}
