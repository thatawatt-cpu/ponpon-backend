using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Promotion.Application;

namespace PonPon.Modules.Promotion.Controllers;

[ApiController]
[Route("api/shop/coupons")]
[Authorize]
public sealed class ShopCouponsController : ControllerBase
{
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyCollection<ShopCouponResponse>>> GetAvailable(
        [FromQuery] ShopCouponRequest request,
        [FromServices] IShopCouponService coupons,
        CancellationToken cancellationToken)
        => Ok(await coupons.GetAvailableAsync(request.ToQuery(), cancellationToken));

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyCollection<ShopCouponResponse>>> GetMyCoupons(
        [FromQuery] ShopCouponRequest request,
        [FromServices] IShopCouponService coupons,
        CancellationToken cancellationToken)
        => Ok(await coupons.GetMyCouponsAsync(request.ToQuery(), cancellationToken));

    [HttpPost("{couponId:guid}/claim")]
    public async Task<ActionResult<ShopCouponResponse>> Claim(
        Guid couponId,
        [FromQuery] ShopCouponRequest request,
        [FromServices] IShopCouponService coupons,
        CancellationToken cancellationToken)
        => Ok(await coupons.ClaimAsync(couponId, request.ToQuery(), cancellationToken));
}

public sealed record ShopCouponRequest(
    string? SalesChannel = null,
    string? PaymentMethod = null,
    string? ShippingChannel = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
    string? Sku = null,
    long? ZortCategoryId = null,
    string? CategoryName = null)
{
    public ShopCouponQuery ToQuery()
        => new(SalesChannel, PaymentMethod, ShippingChannel, ProductId, VariantId, Sku, ZortCategoryId, CategoryName);
}
