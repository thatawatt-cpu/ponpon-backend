using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed record ProductListItemReadModel(
    Guid Id,
    string Name,
    string? BaseSku,
    string? Slug,
    decimal SellPrice,
    decimal? OriginalPrice,
    int Stock,
    int AvailableStock,
    string? ImageUrl,
    string? CategoryName,
    bool IsFeatured,
    bool IsBestSeller,
    string? PromotionBadge,
    bool IsActiveFromZort,
    bool IsVisibleOnLiff,
    ProductSource Source,
    ProductStatus Status,
    int VariantStock,
    int VariantAvailableStock,
    int VariantCount,
    IReadOnlyCollection<string> VariantImages);

public sealed record ProductListPageReadModel(
    IReadOnlyCollection<ProductListItemReadModel> Items,
    int Total);
