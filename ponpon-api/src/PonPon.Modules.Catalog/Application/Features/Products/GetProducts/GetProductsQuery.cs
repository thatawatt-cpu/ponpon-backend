using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed record GetProductsQuery(string? Keyword, string? Category, ProductStatus? Status, ProductSource? Source, int Page = 1, int PageSize = 20, bool IncludeInactive = false);
