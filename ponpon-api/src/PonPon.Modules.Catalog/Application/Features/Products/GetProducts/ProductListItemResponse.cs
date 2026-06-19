using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed record ProductListItemResponse(
    Guid Id,
    string Name,
    string? BaseSku,
    string? Slug,
    decimal SellPrice,
    int Stock,
    int AvailableStock,
    string? ImageUrl,
    string? CategoryName,
    bool IsActiveFromZort,
    bool IsVisibleOnLiff,
    int VariantCount,
    IReadOnlyCollection<string> VariantImages,
    ProductSource Source,
    ProductStatus Status);
