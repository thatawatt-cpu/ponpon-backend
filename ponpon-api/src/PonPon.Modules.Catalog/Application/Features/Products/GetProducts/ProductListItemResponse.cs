using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public sealed record ProductListItemResponse(
    Guid Id,
    string Name,
    string? BaseSku,
    string? Slug,
    decimal SellPrice,
    decimal DisplayPrice,
    decimal? DisplayOriginalPrice,
    string PriceSource,
    Guid? ActiveFlashSaleId,
    int Stock,
    int AvailableStock,
    int SoldCount,
    decimal? Rating,
    int ReviewCount,
    IReadOnlyCollection<string> Badges,
    string? ImageUrl,
    string? CategoryName,
    bool IsFeatured,
    bool IsBestSeller,
    string? PromotionBadge,
    bool IsActiveFromZort,
    bool IsVisibleOnLiff,
    int VariantCount,
    IReadOnlyCollection<string> VariantImages,
    ProductSource Source,
    ProductStatus Status);

public sealed record ProductListPageResponse(
    IReadOnlyCollection<ProductListItemResponse> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);
