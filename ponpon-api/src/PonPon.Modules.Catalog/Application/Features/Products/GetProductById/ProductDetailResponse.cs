using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProductById;

public sealed record ProductDetailResponse(
    Guid Id,
    long? ZortProductId,
    int ProductType,
    string Name,
    string? Description,
    string? BaseSku,
    string? Barcode,
    decimal SellPrice,
    int SellVatStatus,
    decimal? PurchasePrice,
    int PurchaseVatStatus,
    int Stock,
    int AvailableStock,
    string? UnitText,
    string? ImageUrl,
    decimal? Weight,
    decimal? Height,
    decimal? Length,
    decimal? Width,
    long? ZortCategoryId,
    string? CategoryName,
    long? ZortSubCategoryId,
    string? SubCategoryName,
    long? ZortVariationId,
    bool IsActiveFromZort,
    bool IsVisibleOnLiff,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsOnHomepage,
    string? Slug,
    decimal? OriginalPrice,
    string? PromotionBadge,
    string? Highlights,
    string? RichDescription,
    ProductSource Source,
    ProductStatus Status,
    DateTime? LastSyncedAt,
    DateTime? MissingFromZortAt,
    IReadOnlyCollection<ProductImageResponse> Images,
    IReadOnlyCollection<ProductVariantResponse> Variants);

public sealed record ProductImageResponse(
    Guid Id,
    string Url,
    int SortOrder,
    bool IsPrimary);

public sealed record ProductVariantOptionResponse(string Name, string Value);

public sealed record ProductVariantResponse(
    Guid Id,
    long? ZortProductId,
    long? ZortVariationId,
    string Sku,
    string? VariantCode,
    string? Barcode,
    decimal SellPrice,
    int Stock,
    int AvailableStock,
    string? UnitText,
    string? ImageUrl,
    bool IsActiveFromZort,
    ProductStatus Status,
    IReadOnlyCollection<ProductVariantOptionResponse> Options);
