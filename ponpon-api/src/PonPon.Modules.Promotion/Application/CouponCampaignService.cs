using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Domain;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Promotion.Application;

public sealed class CouponCampaignService(PromotionDbContext db) : ICouponCampaignService
{
    public async Task<IReadOnlyCollection<CouponCampaignSummary>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var campaigns = await db.CouponCampaigns.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return await BuildSummariesAsync(campaigns, cancellationToken);
    }

    public async Task<CouponCampaignSummary?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var campaign = await db.CouponCampaigns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (campaign is null)
            return null;
        return (await BuildSummariesAsync([campaign], cancellationToken)).Single();
    }

    public async Task<Guid> CreateAsync(
        CouponCampaignInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        var campaign = CouponCampaign.Create(input, DateTime.UtcNow);
        await db.CouponCampaigns.AddAsync(campaign, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return campaign.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        CouponCampaignInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        var campaign = await db.CouponCampaigns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coupon campaign was not found.");
        campaign.Update(input, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var campaign = await db.CouponCampaigns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Coupon campaign was not found.");
        if (await db.Coupons.AnyAsync(x => x.CampaignId == id && !x.IsDeleted, cancellationToken)
            || await db.Promotions.AnyAsync(x => x.CampaignId == id, cancellationToken))
            campaign.Deactivate(DateTime.UtcNow);
        else
            db.CouponCampaigns.Remove(campaign);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<CouponCampaignSummary>> BuildSummariesAsync(
        IReadOnlyCollection<CouponCampaign> campaigns,
        CancellationToken cancellationToken)
    {
        if (campaigns.Count == 0)
            return [];

        var ids = campaigns.Select(x => x.Id).ToArray();
        var coupons = await db.Coupons.AsNoTracking()
            .Where(x => x.CampaignId.HasValue && ids.Contains(x.CampaignId.Value) && !x.IsDeleted)
            .Select(x => new { x.Id, CampaignId = x.CampaignId!.Value })
            .ToArrayAsync(cancellationToken);
        var couponIds = coupons.Select(x => x.Id).ToArray();
        var usages = couponIds.Length == 0
            ? []
            : await db.CouponUsages.AsNoTracking()
                .Where(x => couponIds.Contains(x.CouponId) && !x.IsReleased)
                .Select(x => new { x.CouponId, x.DiscountAmount })
                .ToArrayAsync(cancellationToken);
        var promotions = await db.Promotions.AsNoTracking()
            .Where(x => x.CampaignId.HasValue && ids.Contains(x.CampaignId.Value))
            .Select(x => new { x.Id, CampaignId = x.CampaignId!.Value, x.IsActive })
            .ToArrayAsync(cancellationToken);
        var promotionIds = promotions.Select(x => x.Id).ToArray();
        var promotionUsages = promotionIds.Length == 0
            ? []
            : await db.PromotionUsages.AsNoTracking()
                .Where(x => promotionIds.Contains(x.PromotionId) && !x.IsReleased)
                .Select(x => new { x.PromotionId, x.DiscountAmount })
                .ToArrayAsync(cancellationToken);

        return campaigns.Select(campaign =>
        {
            var campaignCouponIds = coupons
                .Where(x => x.CampaignId == campaign.Id)
                .Select(x => x.Id)
                .ToHashSet();
            var campaignUsages = usages.Where(x => campaignCouponIds.Contains(x.CouponId)).ToArray();
            var generated = campaignCouponIds.Count;
            var redeemed = campaignUsages.Select(x => x.CouponId).Distinct().Count();
            var campaignPromotions = promotions.Where(x => x.CampaignId == campaign.Id).ToArray();
            var campaignPromotionIds = campaignPromotions.Select(x => x.Id).ToHashSet();
            var campaignPromotionUsages = promotionUsages
                .Where(x => campaignPromotionIds.Contains(x.PromotionId))
                .ToArray();
            return new CouponCampaignSummary(
                campaign,
                generated,
                redeemed,
                generated - redeemed,
                campaignUsages.Length,
                campaignUsages.Sum(x => x.DiscountAmount),
                campaignPromotions.Length,
                campaignPromotions.Count(x => x.IsActive),
                campaignPromotionUsages.Length,
                campaignPromotionUsages.Sum(x => x.DiscountAmount));
        }).ToArray();
    }

    private static void Validate(CouponCampaignInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 256)
            throw new BadRequestException("Campaign name is required and must not exceed 256 characters.");
        if (input.Description?.Trim().Length > 2000)
            throw new BadRequestException("Campaign description must not exceed 2000 characters.");
        if (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue
            && input.StartsAtUtc.Value >= input.EndsAtUtc.Value)
            throw new BadRequestException("Campaign start date must be earlier than end date.");
    }
}
