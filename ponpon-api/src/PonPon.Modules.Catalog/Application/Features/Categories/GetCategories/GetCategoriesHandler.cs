using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesHandler
{
    private readonly IProductRepository _products;

    public GetCategoriesHandler(IProductRepository products) => _products = products;

    public async Task<IReadOnlyCollection<CategoryResponse>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await _products.GetActiveCategoriesAsync(cancellationToken);
        return categories.Select(x => new CategoryResponse(x.Id, x.ZortCategoryId, x.Name)).ToArray();
    }
}
