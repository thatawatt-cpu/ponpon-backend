using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductById;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Products.GetProductBySlug;

public sealed class GetProductBySlugHandler
{
    private readonly IProductRepository _products;
    private readonly PonPon.Shared.Application.Abstractions.IProductSalesReadService _sales;
    private readonly ProductDetailPriceResolver _priceResolver;
    private readonly IMemoryCache _cache;

    public GetProductBySlugHandler(IProductRepository products, PonPon.Shared.Application.Abstractions.IProductSalesReadService sales, ProductDetailPriceResolver priceResolver, IMemoryCache cache)
    {
        _products = products;
        _sales = sales;
        _priceResolver = priceResolver;
        _cache = cache;
    }

    public async Task<ProductDetailResponse> HandleAsync(GetProductBySlugQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"catalog:product-detail:slug:{query.Slug.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out ProductDetailResponse? cached)
            && cached is not null)
        {
            return cached;
        }

        var product = await _products.GetDetailBySlugAsync(query.Slug, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (!product.IsVisibleToCustomer)
            throw new NotFoundException("Product was not found.");

        var soldCountsTask = _sales.GetSoldCountsAsync([product.Id], cancellationToken);
        var priceTask = _priceResolver.ResolveAsync(product, cancellationToken);
        await Task.WhenAll(soldCountsTask, priceTask);
        var soldCounts = await soldCountsTask;
        var price = await priceTask;

        var response = new ProductDetailResponse(
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
            soldCounts.GetValueOrDefault(product.Id),
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
            price.DisplayPrice,
            price.DisplayOriginalPrice,
            price.PriceSource,
            price.ActiveFlashSaleId,
            product.PromotionBadge,
            product.Highlights,
            product.RichDescription,
            product.Source,
            product.Status,
            product.LastSyncedAt,
            product.MissingFromZortAt,
            product.Images.Select(x => new ProductImageResponse(x.Id, x.Url, x.SortOrder, x.IsPrimary)).ToArray(),
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
        _cache.Set(cacheKey, response, ProductDetailCacheOptions);
        return response;
    }

    private static readonly MemoryCacheEntryOptions ProductDetailCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20),
        SlidingExpiration = TimeSpan.FromSeconds(10),
        Size = 1
    };
}
