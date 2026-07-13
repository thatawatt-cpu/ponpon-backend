using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetActiveFlashSale;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Modules.Catalog.Application.Features.HomeSlides;
using PonPon.Modules.Catalog.Application.Features.HomeSlides.GetPublishedHomeSlides;
using PonPon.Modules.Catalog.Application.Features.Products;
using PonPon.Modules.Catalog.Application.Features.Products.GetProducts;
using PonPon.Modules.Promotion.Application;
using PonPon.Shared.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PonPon.Api.Controllers;

[ApiController]
public sealed class ShopHomeController : ControllerBase
{
    [HttpGet("api/shop/home")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopHomeResponse>> GetHome(
        [FromQuery] ShopHomeRequest request,
        [FromServices] GetPublishedHomeSlidesHandler slidesHandler,
        [FromServices] GetActiveFlashSaleHandler flashSaleHandler,
        [FromServices] IProductRepository products,
        [FromServices] IProductSalesReadService sales,
        [FromServices] ProductDetailPriceResolver priceResolver,
        [FromServices] IShopCouponService coupons,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var featuredLimit = Math.Clamp(request.FeaturedProductLimit, 1, 40);
        var slides = await slidesHandler.HandleAsync(cancellationToken);
        var flashSale = await flashSaleHandler.HandleAsync(cancellationToken);
        var featuredProducts = await products.GetFeaturedCustomerProductsAsync(featuredLimit, cancellationToken);
        var soldCounts = await sales.GetSoldCountsAsync(
            featuredProducts.Select(x => x.Id).ToArray(),
            cancellationToken);
        var prices = await priceResolver.ResolveAsync(featuredProducts, cancellationToken);
        var hasCustomerContext = currentUser.IsAuthenticated && currentUser.UserType == "Customer";
        var availableCoupons = hasCustomerContext
            ? await coupons.GetAvailableAsync(new ShopCouponQuery(request.SalesChannel), cancellationToken)
            : [];

        var response = new ShopHomeResponse(
            slides,
            flashSale,
            featuredProducts.Select(x => ProductListItemResponseFactory.Create(x, soldCounts, prices)).ToArray(),
            availableCoupons);

        if (hasCustomerContext)
        {
            Response.Headers["Cache-Control"] = "private, no-store";
            return Ok(response);
        }

        Response.Headers["Cache-Control"] = "public, max-age=60, s-maxage=60, stale-while-revalidate=30";
        var etag = CreateWeakEtag(response);
        Response.Headers["ETag"] = etag;
        if (Request.Headers.IfNoneMatch.Any(x => string.Equals(x, etag, StringComparison.Ordinal)))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(response);
    }

    private static string CreateWeakEtag<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return $"W/\"{hash}\"";
    }
}

public sealed record ShopHomeRequest(
    string? SalesChannel = null,
    int FeaturedProductLimit = 12);

public sealed record ShopHomeResponse(
    IReadOnlyCollection<HomeSlideResponse> Slides,
    FlashSaleResponse? FlashSale,
    IReadOnlyCollection<ProductListItemResponse> FeaturedProducts,
    IReadOnlyCollection<ShopCouponResponse> AvailableCoupons);
