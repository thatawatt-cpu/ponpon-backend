using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;
using PonPon.Shared.Application.Abstractions;
using PromotionEntity = PonPon.Modules.Promotion.Domain.Promotion;

namespace PonPon.Modules.Promotion.Controllers;

[ApiController]
[Route("api/admin/promotions")]
[Authorize(Roles = "Admin")]
public sealed class AdminPromotionsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PromotionResponse>>> GetAll(
        [FromQuery] Guid? campaignId,
        [FromServices] IPromotionService promotions,
        CancellationToken cancellationToken)
        => Ok((await promotions.GetAllAsync(campaignId, cancellationToken)).Select(PromotionResponse.From));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromotionResponse>> GetById(
        Guid id,
        [FromServices] IPromotionService promotions,
        CancellationToken cancellationToken)
    {
        var promotion = await promotions.GetByIdAsync(id, cancellationToken);
        return promotion is null ? NotFound() : Ok(PromotionResponse.From(promotion));
    }

    [HttpGet("{id:guid}/usages")]
    public async Task<ActionResult<IReadOnlyCollection<PromotionUsageResponse>>> GetUsages(
        Guid id,
        [FromServices] IPromotionService promotions,
        [FromServices] IOrderUsageReadService orders,
        CancellationToken cancellationToken)
    {
        var usages = await promotions.GetUsagesAsync(id, cancellationToken);
        var orderDetails = await orders.GetByIdsAsync(
            usages.Select(x => x.OrderId).ToArray(),
            cancellationToken);

        return Ok(usages.Select(x =>
        {
            var order = orderDetails.GetValueOrDefault(x.OrderId);
            return new PromotionUsageResponse(
                x.Id,
                x.OrderId,
                x.CustomerId,
                x.DiscountAmount,
                x.IsReleased,
                x.CreatedAtUtc,
                x.ReleasedAtUtc,
                order?.OrderNumber ?? string.Empty,
                order?.CustomerName ?? string.Empty,
                order?.CustomerPhone,
                order?.OrderTotal,
                x.PromotionName,
                x.CreatedAtUtc);
        }));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] PromotionRequest request,
        [FromServices] IPromotionService promotions,
        CancellationToken cancellationToken)
    {
        var id = await promotions.CreateAsync(request.ToInput(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] PromotionRequest request,
        [FromServices] IPromotionService promotions,
        CancellationToken cancellationToken)
    {
        await promotions.UpdateAsync(id, request.ToInput(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IPromotionService promotions,
        CancellationToken cancellationToken)
    {
        await promotions.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed record PromotionRequest(
    string Name,
    string? Description,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    string? Timezone,
    int Priority,
    bool CanStackWithCoupon,
    bool CanStackWithPromotions,
    bool CanCombineWithFlashSale,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    bool IsActive,
    IReadOnlyCollection<PromotionScheduleRuleRequest>? ScheduleRules = null,
    IReadOnlyCollection<PromotionScopeRequest>? Scopes = null,
    IReadOnlyCollection<PromotionCustomerScopeRequest>? CustomerScopes = null,
    IReadOnlyCollection<PromotionConditionRequest>? Conditions = null,
    Guid? CampaignId = null)
{
    public PromotionInput ToInput() => new(
        Name,
        Description,
        Type,
        DiscountType,
        DiscountValue,
        MinimumSubtotal,
        MaximumDiscount,
        StartsAtUtc,
        EndsAtUtc,
        Timezone,
        Priority,
        CanStackWithCoupon,
        CanStackWithPromotions,
        CanCombineWithFlashSale,
        MaximumTotalUses,
        MaximumUsesPerCustomer,
        IsActive,
        ScheduleRules?.Select(x => new PromotionScheduleRuleInput(
            x.Type,
            x.DayOfWeek,
            x.DayOfMonth,
            x.StartsAtLocalTime,
            x.EndsAtLocalTime)).ToArray(),
        Scopes?.Select(x => new PromotionScopeInput(
            x.Type,
            x.ProductId,
            x.VariantId,
            x.Sku,
            x.CategoryName,
            x.IsExclude)).ToArray(),
        CustomerScopes?.Select(x => new PromotionCustomerScopeInput(x.Type, x.CustomerId)).ToArray(),
        Conditions?.Select(x => new PromotionConditionInput(x.Type, x.Value)).ToArray(),
        CampaignId);
}

public sealed record PromotionScheduleRuleRequest(
    string Type,
    int? DayOfWeek,
    int? DayOfMonth,
    TimeOnly? StartsAtLocalTime,
    TimeOnly? EndsAtLocalTime);

public sealed record PromotionScopeRequest(
    string Type,
    Guid? ProductId,
    Guid? VariantId,
    string? Sku,
    string? CategoryName,
    bool IsExclude);

public sealed record PromotionCustomerScopeRequest(string Type, Guid? CustomerId);

public sealed record PromotionConditionRequest(string Type, string Value);

public sealed record PromotionUsageResponse(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal DiscountAmount,
    bool IsReleased,
    DateTime CreatedAtUtc,
    DateTime? ReleasedAtUtc,
    string OrderNumber,
    string CustomerName,
    string? CustomerPhone,
    decimal? OrderTotal,
    string PromotionName,
    DateTime UsedAt);

public sealed record PromotionResponse(
    Guid Id,
    Guid? CampaignId,
    string Name,
    string? Description,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    string Timezone,
    int Priority,
    bool CanStackWithCoupon,
    bool CanStackWithPromotions,
    bool CanCombineWithFlashSale,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    int UsedCount,
    bool IsActive,
    IReadOnlyCollection<PromotionScheduleRuleResponse> ScheduleRules,
    IReadOnlyCollection<PromotionScopeResponse> Scopes,
    IReadOnlyCollection<PromotionCustomerScopeResponse> CustomerScopes,
    IReadOnlyCollection<PromotionConditionResponse> Conditions)
{
    public static PromotionResponse From(PromotionEntity promotion) => new(
        promotion.Id,
        promotion.CampaignId,
        promotion.Name,
        promotion.Description,
        promotion.Type,
        promotion.DiscountType,
        promotion.DiscountValue,
        promotion.MinimumSubtotal,
        promotion.MaximumDiscount,
        promotion.StartsAtUtc,
        promotion.EndsAtUtc,
        promotion.Timezone,
        promotion.Priority,
        promotion.CanStackWithCoupon,
        promotion.CanStackWithPromotions,
        promotion.CanCombineWithFlashSale,
        promotion.MaximumTotalUses,
        promotion.MaximumUsesPerCustomer,
        promotion.UsedCount,
        promotion.IsActive,
        promotion.ScheduleRules.Select(x => new PromotionScheduleRuleResponse(
            x.Id,
            x.Type,
            x.DayOfWeek,
            x.DayOfMonth,
            x.StartsAtLocalTime,
            x.EndsAtLocalTime)).ToArray(),
        promotion.Scopes.Select(x => new PromotionScopeResponse(
            x.Id,
            x.Type,
            x.ProductId,
            x.VariantId,
            x.Sku,
            x.CategoryName,
            x.IsExclude)).ToArray(),
        promotion.CustomerScopes.Select(x => new PromotionCustomerScopeResponse(
            x.Id,
            x.Type,
            x.CustomerId)).ToArray(),
        promotion.Conditions.Select(x => new PromotionConditionResponse(
            x.Id,
            x.Type,
            x.Value)).ToArray());
}

public sealed record PromotionScheduleRuleResponse(
    Guid Id,
    string Type,
    int? DayOfWeek,
    int? DayOfMonth,
    TimeOnly? StartsAtLocalTime,
    TimeOnly? EndsAtLocalTime);

public sealed record PromotionScopeResponse(
    Guid Id,
    string Type,
    Guid? ProductId,
    Guid? VariantId,
    string? Sku,
    string? CategoryName,
    bool IsExclude);

public sealed record PromotionCustomerScopeResponse(Guid Id, string Type, Guid? CustomerId);

public sealed record PromotionConditionResponse(Guid Id, string Type, string Value);
