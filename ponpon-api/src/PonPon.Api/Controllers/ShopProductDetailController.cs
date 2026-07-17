using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductById;
using PonPon.Modules.Catalog.Application.Features.Products.GetProductBySlug;
using PonPon.Modules.Catalog.Application.Features.Products.GetProducts;
using PonPon.Modules.Promotion.Application;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Api.Controllers;

[ApiController]
public sealed class ShopProductDetailController : ControllerBase
{
    [HttpGet("api/shop/products/{id:guid}/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopProductSummaryResponse>> GetSummaryById(
        Guid id,
        [FromServices] GetProductByIdHandler productHandler,
        CancellationToken cancellationToken)
    {
        var product = await productHandler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
        return Ok(BuildSummaryResponse(product));
    }

    [HttpGet("api/shop/products/slug/{slug}/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopProductSummaryResponse>> GetSummaryBySlug(
        string slug,
        [FromServices] GetProductBySlugHandler productHandler,
        CancellationToken cancellationToken)
    {
        var product = await productHandler.HandleAsync(new GetProductBySlugQuery(slug), cancellationToken);
        return Ok(BuildSummaryResponse(product));
    }

    [HttpGet("api/shop/products/{id:guid}/detail")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopProductDetailResponse>> GetById(
        Guid id,
        [FromQuery] ShopProductDetailRequest request,
        [FromServices] GetProductByIdHandler productHandler,
        [FromServices] IProductRepository products,
        [FromServices] IProductSalesReadService sales,
        [FromServices] ProductDetailPriceResolver priceResolver,
        [FromServices] IShopCouponService coupons,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var product = await productHandler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);
        return Ok(await BuildResponseAsync(
            product,
            request,
            products,
            sales,
            priceResolver,
            coupons,
            currentUser,
            cancellationToken));
    }

    [HttpGet("api/shop/products/slug/{slug}/detail")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopProductDetailResponse>> GetBySlug(
        string slug,
        [FromQuery] ShopProductDetailRequest request,
        [FromServices] GetProductBySlugHandler productHandler,
        [FromServices] IProductRepository products,
        [FromServices] IProductSalesReadService sales,
        [FromServices] ProductDetailPriceResolver priceResolver,
        [FromServices] IShopCouponService coupons,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var product = await productHandler.HandleAsync(new GetProductBySlugQuery(slug), cancellationToken);
        return Ok(await BuildResponseAsync(
            product,
            request,
            products,
            sales,
            priceResolver,
            coupons,
            currentUser,
            cancellationToken));
    }

    private static async Task<ShopProductDetailResponse> BuildResponseAsync(
        ProductDetailResponse product,
        ShopProductDetailRequest request,
        IProductRepository products,
        IProductSalesReadService sales,
        ProductDetailPriceResolver priceResolver,
        IShopCouponService coupons,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var relatedLimit = Math.Clamp(request.RelatedProductLimit, 1, 40);
        var relatedProducts = await products.GetRelatedCustomerProductListItemsAsync(
            product.Id,
            product.CategoryName,
            relatedLimit,
            cancellationToken);
        var relatedProductIds = relatedProducts.Select(x => x.Id).ToArray();
        var soldCountsTask = sales.GetSoldCountsAsync(relatedProductIds, cancellationToken);
        var relatedPricesTask = priceResolver.ResolveAsync(relatedProducts, cancellationToken);
        Task<IReadOnlyCollection<ShopCouponResponse>> availableCouponsTask = currentUser.IsAuthenticated && currentUser.UserType == "Customer"
            ? coupons.GetAvailableAsync(
                new ShopCouponQuery(
                    request.SalesChannel,
                    ProductId: product.Id,
                    Sku: product.BaseSku,
                    ZortCategoryId: product.ZortCategoryId,
                    CategoryName: product.CategoryName),
                cancellationToken)
            : Task.FromResult<IReadOnlyCollection<ShopCouponResponse>>([]);

        await Task.WhenAll(soldCountsTask, relatedPricesTask, availableCouponsTask);
        var soldCounts = await soldCountsTask;
        var relatedPrices = await relatedPricesTask;
        var availableCoupons = await availableCouponsTask;

        return new ShopProductDetailResponse(
            product,
            availableCoupons,
            relatedProducts.Select(x => ProductListItemResponseFactory.Create(x, soldCounts, relatedPrices)).ToArray(),
            new ResolvedProductPriceResponse(
                product.DisplayPrice,
                product.DisplayOriginalPrice,
                product.PriceSource,
                product.ActiveFlashSaleId));
    }

    private static ShopProductSummaryResponse BuildSummaryResponse(ProductDetailResponse product)
    {
        var primaryImage = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .FirstOrDefault();
        var imageUrl = primaryImage?.Url ?? product.ImageUrl;
        var activeVariants = product.Variants
            .Where(x => x.IsActiveFromZort)
            .Select(x => new ShopProductSummaryVariantResponse(
                x.Id,
                x.Sku,
                x.VariantCode,
                x.SellPrice,
                x.Stock,
                x.AvailableStock,
                x.ImageUrl,
                x.Options))
            .ToArray();

        return new ShopProductSummaryResponse(
            product.Id,
            product.Slug,
            product.Name,
            product.BaseSku,
            product.CategoryName,
            product.ZortCategoryId,
            imageUrl,
            product.DisplayPrice,
            product.DisplayOriginalPrice,
            product.PriceSource,
            product.ActiveFlashSaleId,
            product.Stock,
            product.AvailableStock,
            product.SoldCount,
            product.PromotionBadge,
            product.Highlights,
            activeVariants,
            new ResolvedProductPriceResponse(
                product.DisplayPrice,
                product.DisplayOriginalPrice,
                product.PriceSource,
                product.ActiveFlashSaleId));
    }
}

public sealed record ShopProductDetailRequest(
    string? SalesChannel = null,
    int RelatedProductLimit = 8);

public sealed record ShopProductDetailResponse(
    ProductDetailResponse Product,
    IReadOnlyCollection<ShopCouponResponse> AvailableCoupons,
    IReadOnlyCollection<ProductListItemResponse> RelatedProducts,
    ResolvedProductPriceResponse ResolvedPrice);

public sealed record ShopProductSummaryResponse(
    Guid Id,
    string? Slug,
    string Name,
    string? BaseSku,
    string? CategoryName,
    long? ZortCategoryId,
    string? ImageUrl,
    decimal DisplayPrice,
    decimal? DisplayOriginalPrice,
    string PriceSource,
    Guid? ActiveFlashSaleId,
    int Stock,
    int AvailableStock,
    int SoldCount,
    string? PromotionBadge,
    string? Highlights,
    IReadOnlyCollection<ShopProductSummaryVariantResponse> Variants,
    ResolvedProductPriceResponse ResolvedPrice);

public sealed record ShopProductSummaryVariantResponse(
    Guid Id,
    string Sku,
    string? VariantCode,
    decimal SellPrice,
    int Stock,
    int AvailableStock,
    string? ImageUrl,
    IReadOnlyCollection<ProductVariantOptionResponse> Options);

public sealed record ResolvedProductPriceResponse(
    decimal DisplayPrice,
    decimal? DisplayOriginalPrice,
    string PriceSource,
    Guid? ActiveFlashSaleId);
