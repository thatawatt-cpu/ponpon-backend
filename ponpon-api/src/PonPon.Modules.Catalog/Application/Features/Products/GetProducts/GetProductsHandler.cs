using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler
{
    private readonly IProductRepository _products;

    public GetProductsHandler(IProductRepository products) => _products = products;

    public async Task<IReadOnlyCollection<ProductListItemResponse>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var products = query.IncludeInactive
            ? await _products.GetAdminProductsAsync(query.Keyword, query.Status, query.Source, page, pageSize, cancellationToken)
            : await _products.GetCustomerProductsAsync(query.Keyword, query.Category, page, pageSize, cancellationToken);

        return products.Select(x => new ProductListItemResponse(
            x.Id,
            x.Name,
            x.BaseSku,
            x.Slug,
            x.SellPrice,
            x.Variants.Sum(v => v.Stock),
            x.Variants.Sum(v => v.AvailableStock),
            x.ImageUrl,
            x.CategoryName,
            x.IsActiveFromZort,
            x.IsVisibleOnLiff,
            x.Variants.Count,
            x.Variants.Select(v => v.ImageUrl).OfType<string>().ToArray(),
            x.Source,
            x.Status)).ToArray();
    }
}
