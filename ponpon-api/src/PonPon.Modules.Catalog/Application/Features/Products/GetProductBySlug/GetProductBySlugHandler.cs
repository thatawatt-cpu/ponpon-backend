using System.Text.Json;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductById;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProductBySlug;

public sealed class GetProductBySlugHandler
{
    private readonly IProductRepository _products;

    public GetProductBySlugHandler(IProductRepository products) => _products = products;

    public async Task<ProductDetailResponse> HandleAsync(GetProductBySlugQuery query, CancellationToken cancellationToken = default)
    {
            var product = await _products.GetBySlugWithVariantsAndImagesAsync(query.Slug, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (!product.IsVisibleToCustomer)
            throw new NotFoundException("Product was not found.");

        return new ProductDetailResponse(
            product.Id,
            product.ZortProductId,
            product.ProductType,
            product.Name,
            product.Description,
            product.BaseSku,
            product.Barcode,
            product.SellPrice,
            product.SellVatStatus,
            product.PurchasePrice,
            product.PurchaseVatStatus,
            product.Stock,
            product.AvailableStock,
            product.UnitText,
            product.ImageUrl,
            product.Weight,
            product.Height,
            product.Length,
            product.Width,
            product.ZortCategoryId,
            product.CategoryName,
            product.ZortSubCategoryId,
            product.SubCategoryName,
            product.ZortVariationId,
            product.IsActiveFromZort,
            product.IsVisibleOnLiff,
            product.IsFeatured,
            product.IsBestSeller,
            product.IsOnHomepage,
            product.Slug,
            product.OriginalPrice,
            product.PromotionBadge,
            product.Highlights,
            product.RichDescription,
            product.Source,
            product.Status,
            product.LastSyncedAt,
            product.MissingFromZortAt,
            product.Images.OrderBy(x => x.SortOrder).Select(x => new ProductImageResponse(x.Id, x.Url, x.SortOrder, x.IsPrimary)).ToArray(),
            product.Variants.Select(x => new ProductVariantResponse(
                x.Id,
                x.ZortProductId,
                x.ZortVariationId,
                x.Sku,
                x.VariantCode,
                x.Barcode,
                x.SellPrice,
                x.Stock,
                x.AvailableStock,
                x.UnitText,
                x.ImageUrl,
                x.IsActiveFromZort,
                x.Status,
                x.OptionsJson is not null
                    ? JsonSerializer.Deserialize<ProductVariantOptionResponse[]>(x.OptionsJson) ?? []
                    : [])).ToArray());
    }
}
