using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;

namespace PonPon.Modules.Promotion.Controllers;

[ApiController]
[Route("api/admin/coupon-campaigns")]
[Authorize(Roles = "Admin")]
public sealed class AdminCouponCampaignsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CouponCampaignResponse>>> GetAll(
        [FromServices] ICouponCampaignService campaigns,
        CancellationToken cancellationToken)
        => Ok((await campaigns.GetAllAsync(cancellationToken)).Select(CouponCampaignResponse.From));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CouponCampaignResponse>> GetById(
        Guid id,
        [FromServices] ICouponCampaignService campaigns,
        CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(id, cancellationToken);
        return campaign is null ? NotFound() : Ok(CouponCampaignResponse.From(campaign));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CouponCampaignRequest request,
        [FromServices] ICouponCampaignService campaigns,
        CancellationToken cancellationToken)
    {
        var id = await campaigns.CreateAsync(request.ToInput(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CouponCampaignRequest request,
        [FromServices] ICouponCampaignService campaigns,
        CancellationToken cancellationToken)
    {
        await campaigns.UpdateAsync(id, request.ToInput(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICouponCampaignService campaigns,
        CancellationToken cancellationToken)
    {
        await campaigns.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed record CouponCampaignRequest(
    string Name,
    string? Description,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool IsActive)
{
    public CouponCampaignInput ToInput()
        => new(Name, Description, StartsAtUtc, EndsAtUtc, IsActive);
}

public sealed record CouponCampaignResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int GeneratedCoupons,
    int RedeemedCoupons,
    int RemainingCoupons,
    int ActiveUsageCount,
    decimal TotalDiscountAmount,
    int PromotionCount,
    int ActivePromotionCount,
    int PromotionUsageCount,
    decimal PromotionDiscountAmount)
{
    public static CouponCampaignResponse From(CouponCampaignSummary x) => new(
        x.Campaign.Id,
        x.Campaign.Name,
        x.Campaign.Description,
        x.Campaign.StartsAtUtc,
        x.Campaign.EndsAtUtc,
        x.Campaign.IsActive,
        x.Campaign.CreatedAtUtc,
        x.Campaign.UpdatedAtUtc,
        x.GeneratedCoupons,
        x.RedeemedCoupons,
        x.RemainingCoupons,
        x.ActiveUsageCount,
        x.TotalDiscountAmount,
        x.PromotionCount,
        x.ActivePromotionCount,
        x.PromotionUsageCount,
        x.PromotionDiscountAmount);
}
