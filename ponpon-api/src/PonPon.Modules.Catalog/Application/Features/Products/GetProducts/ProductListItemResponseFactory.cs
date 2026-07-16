using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProducts;

public static class ProductListItemResponseFactory
{
    public static ProductListItemResponse Create(
        Product product,
        IReadOnlyDictionary<Guid, int> soldCounts,
        IReadOnlyDictionary<Guid, ProductDetailPrice> prices)
    {
        var price = prices[product.Id];
        return new ProductListItemResponse(
            product.Id,
            product.Name,
            product.BaseSku,
            product.Slug,
            product.SellPrice,
            price.DisplayPrice,
            price.DisplayOriginalPrice,
            price.PriceSource,
            price.ActiveFlashSaleId,
            product.Variants.Sum(v => v.Stock),
            product.Variants.Sum(v => v.AvailableStock),
            soldCounts.GetValueOrDefault(product.Id),
            null,
            0,
            price.PriceSource == ProductDetailPriceSource.FlashSale ? ["flash_sale"] : [],
            product.ImageUrl,
            product.CategoryName,
            product.IsFeatured,
            product.IsBestSeller,
            product.PromotionBadge,
            product.IsActiveFromZort,
            product.IsVisibleOnLiff,
            product.Variants.Count,
            product.Variants.Select(v => v.ImageUrl).OfType<string>().ToArray(),
            product.Source,
            product.Status);
    }

    public static ProductListItemResponse Create(
        ProductListItemReadModel product,
        IReadOnlyDictionary<Guid, int> soldCounts,
        IReadOnlyDictionary<Guid, ProductDetailPrice> prices)
    {
        var price = prices[product.Id];
        return new ProductListItemResponse(
            product.Id,
            product.Name,
            product.BaseSku,
            product.Slug,
            product.SellPrice,
            price.DisplayPrice,
            price.DisplayOriginalPrice,
            price.PriceSource,
            price.ActiveFlashSaleId,
            product.VariantCount > 0 ? product.VariantStock : product.Stock,
            product.VariantCount > 0 ? product.VariantAvailableStock : product.AvailableStock,
            soldCounts.GetValueOrDefault(product.Id),
            null,
            0,
            price.PriceSource == ProductDetailPriceSource.FlashSale ? ["flash_sale"] : [],
            product.ImageUrl,
            product.CategoryName,
            product.IsFeatured,
            product.IsBestSeller,
            product.PromotionBadge,
            product.IsActiveFromZort,
            product.IsVisibleOnLiff,
            product.VariantCount,
            product.VariantImages,
            product.Source,
            product.Status);
    }
}
