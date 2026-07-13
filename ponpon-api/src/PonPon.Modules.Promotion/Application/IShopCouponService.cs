namespace PonPon.Modules.Promotion.Application;

public interface IShopCouponService
{
    Task<IReadOnlyCollection<ShopCouponResponse>> GetAvailableAsync(
        ShopCouponQuery query,
        CancellationToken cancellationToken = default);

    Task<ShopCouponResponse> ClaimAsync(
        Guid couponId,
        ShopCouponQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShopCouponResponse>> GetMyCouponsAsync(
        ShopCouponQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record ShopCouponQuery(
    string? SalesChannel = null,
    string? PaymentMethod = null,
    string? ShippingChannel = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
    string? Sku = null,
    long? ZortCategoryId = null,
    string? CategoryName = null);

public sealed record ShopCouponResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    int UsedCount,
    int? RemainingTotalUses,
    bool IsClaimed,
    bool CanClaim,
    DateTime? ClaimedAtUtc,
    IReadOnlyCollection<string> ScopeLabels,
    IReadOnlyCollection<string> ConditionLabels);
