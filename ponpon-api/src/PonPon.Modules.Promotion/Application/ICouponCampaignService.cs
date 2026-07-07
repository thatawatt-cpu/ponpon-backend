using PonPon.Modules.Promotion.Domain;

namespace PonPon.Modules.Promotion.Application;

public interface ICouponCampaignService
{
    Task<IReadOnlyCollection<CouponCampaignSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CouponCampaignSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CouponCampaignInput input, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, CouponCampaignInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record CouponCampaignSummary(
    CouponCampaign Campaign,
    int GeneratedCoupons,
    int RedeemedCoupons,
    int RemainingCoupons,
    int ActiveUsageCount,
    decimal TotalDiscountAmount,
    int PromotionCount,
    int ActivePromotionCount,
    int PromotionUsageCount,
    decimal PromotionDiscountAmount);
