using PonPon.Modules.Promotion.Domain;

namespace PonPon.Modules.Promotion.Application;

public interface ICouponService
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Coupon>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Coupon>> GetByCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CouponUsageListItem>> GetUsagesAsync(Guid couponId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CouponAuditLog>> GetAuditLogsAsync(Guid couponId, CancellationToken cancellationToken = default);
    Task<int> GetActiveCustomerUsageCountAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CouponInput input, CancellationToken cancellationToken = default);
    Task<CouponBulkGenerateResult> BulkGenerateAsync(CouponBulkGenerateInput input, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, CouponInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TryReserveAsync(
        Guid couponId,
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default,
        decimal discountAmount = 0);
    Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public sealed record CouponUsageListItem(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal DiscountAmount,
    bool IsReleased,
    DateTime CreatedAtUtc,
    DateTime? ReleasedAtUtc,
    string CouponCode,
    string CouponName);

public sealed record CouponBulkGenerateInput(
    string Prefix,
    int Count,
    int CodeLength,
    CouponTemplateInput Template,
    Guid? CampaignId = null,
    Guid? BatchId = null,
    Guid? ActorUserId = null,
    string? ActorUserType = null);

public sealed record CouponTemplateInput(
    string? Name,
    string? Description,
    string Type,
    decimal Value,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool CanCombineWithFlashSale,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    bool IsActive,
    IReadOnlyCollection<CouponScopeInput>? Scopes = null,
    IReadOnlyCollection<CouponCustomerScopeInput>? CustomerScopes = null,
    IReadOnlyCollection<CouponConditionInput>? Conditions = null,
    bool CanStackWithPromotions = true,
    bool CanStackWithCoupons = true);

public sealed record CouponBulkGenerateResult(
    Guid BatchId,
    Guid? CampaignId,
    int CreatedCount,
    IReadOnlyCollection<string> Codes);
