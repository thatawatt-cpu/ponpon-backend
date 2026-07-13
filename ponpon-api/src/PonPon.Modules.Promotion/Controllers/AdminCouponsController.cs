using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Promotion.Controllers;

[ApiController]
[Route("api/admin/coupons")]
[Authorize(Roles = "Admin")]
public sealed class AdminCouponsController : ControllerBase
{
    [HttpPost("bulk-generate")]
    public async Task<ActionResult<CouponBulkGenerationQueuedResponse>> BulkGenerate(
        [FromBody] CouponBulkGenerateRequest request,
        [FromServices] ICouponBulkGenerationJobService jobs,
        [FromServices] IPersistentBackgroundJobClient backgroundJobs,
        CancellationToken cancellationToken)
    {
        var job = await jobs.QueueAsync(request.ToInput(), cancellationToken);
        try
        {
            var backgroundJobId = backgroundJobs.Enqueue<CouponBulkGenerationBackgroundJob>(
                worker => worker.ExecuteAsync(job.Id));
            await jobs.AttachBackgroundJobAsync(job.Id, backgroundJobId, cancellationToken);
            return Accepted(new CouponBulkGenerationQueuedResponse(
                job.Id,
                job.BatchId,
                backgroundJobId,
                CouponBulkGenerationJobStatus.Pending,
                job.RequestedCount));
        }
        catch (Exception ex)
        {
            await jobs.MarkEnqueueFailedAsync(job.Id, ex.Message, CancellationToken.None);
            throw;
        }
    }

    [HttpGet("bulk-generate-jobs")]
    public async Task<ActionResult<IReadOnlyCollection<CouponBulkGenerationJobResponse>>> GetBulkGenerateJobs(
        [FromQuery] int take,
        [FromServices] ICouponBulkGenerationJobService jobs,
        CancellationToken cancellationToken)
        => Ok((await jobs.GetRecentAsync(take == 0 ? 50 : take, cancellationToken))
            .Select(CouponBulkGenerationJobResponse.From));

    [HttpGet("bulk-generate-jobs/{id:guid}")]
    public async Task<ActionResult<CouponBulkGenerationJobResponse>> GetBulkGenerateJob(
        Guid id,
        [FromServices] ICouponBulkGenerationJobService jobs,
        CancellationToken cancellationToken)
    {
        var job = await jobs.GetAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(CouponBulkGenerationJobResponse.From(job));
    }

    [HttpGet("bulk-generate-jobs/{id:guid}/codes")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetBulkGenerateJobCodes(
        Guid id,
        [FromServices] ICouponBulkGenerationJobService jobs,
        CancellationToken cancellationToken)
        => Ok(await jobs.GetCodesAsync(id, cancellationToken));

    [HttpGet("{id:guid}/usages")]
    public async Task<ActionResult<IReadOnlyCollection<CouponUsageResponse>>> GetUsages(
        Guid id,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
    {
        var usages = await coupons.GetUsagesAsync(id, cancellationToken);
        return Ok(usages.Select(x => new CouponUsageResponse(
            x.Id, x.OrderId, x.CustomerId, x.DiscountAmount,
            x.IsReleased, x.CreatedAtUtc, x.ReleasedAtUtc)));
    }

    [HttpGet("{id:guid}/audit-logs")]
    public async Task<ActionResult<IReadOnlyCollection<CouponAuditLogResponse>>> GetAuditLogs(
        Guid id,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
    {
        var logs = await coupons.GetAuditLogsAsync(id, cancellationToken);
        return Ok(logs.Select(x => new CouponAuditLogResponse(
            x.Id,
            x.CouponId,
            x.BatchId,
            x.Action,
            x.ActorUserId,
            x.ActorUserType,
            x.BeforeJson,
            x.AfterJson,
            x.CreatedAtUtc)));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CouponResponse>>> GetAll(
        [FromQuery] Guid? campaignId,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
        => Ok((campaignId.HasValue
                ? await coupons.GetByCampaignAsync(campaignId.Value, cancellationToken)
                : await coupons.GetAllAsync(cancellationToken))
            .Select(CouponResponse.From));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CouponResponse>> GetById(
        Guid id,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(id, cancellationToken);
        return coupon is null ? NotFound() : Ok(CouponResponse.From(coupon));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CouponRequest request,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
    {
        var id = await coupons.CreateAsync(request.ToInput(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CouponRequest request,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
    {
        await coupons.UpdateAsync(id, request.ToInput(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICouponService coupons,
        CancellationToken cancellationToken)
    {
        await coupons.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed record CouponUsageResponse(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal DiscountAmount,
    bool IsReleased,
    DateTime CreatedAtUtc,
    DateTime? ReleasedAtUtc);

public sealed record CouponAuditLogResponse(
    Guid Id,
    Guid? CouponId,
    Guid? BatchId,
    string Action,
    Guid? ActorUserId,
    string? ActorUserType,
    string? BeforeJson,
    string? AfterJson,
    DateTime CreatedAtUtc);

public sealed record CouponBulkGenerateRequest(
    string Prefix,
    int Count,
    int CodeLength,
    CouponTemplateRequest Template,
    Guid? CampaignId = null)
{
    public CouponBulkGenerateInput ToInput()
        => new(Prefix, Count, CodeLength, Template.ToInput(), CampaignId);
}

public sealed record CouponTemplateRequest(
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
    IReadOnlyCollection<CouponScopeRequest>? Scopes = null,
    IReadOnlyCollection<CouponCustomerScopeRequest>? CustomerScopes = null,
    IReadOnlyCollection<CouponConditionRequest>? Conditions = null,
    bool CanStackWithPromotions = true,
    bool CanStackWithCoupons = true)
{
    public CouponTemplateInput ToInput() => new(
        Name,
        Description,
        Type,
        Value,
        MinimumSubtotal,
        MaximumDiscount,
        StartsAtUtc,
        EndsAtUtc,
        CanCombineWithFlashSale,
        MaximumTotalUses,
        MaximumUsesPerCustomer,
        IsActive,
        Scopes?.Select(x => new CouponScopeInput(
            x.Type, x.ProductId, x.VariantId, x.Sku, CategoryName: x.CategoryName)).ToArray(),
        CustomerScopes?.Select(x => new CouponCustomerScopeInput(x.Type, x.CustomerId)).ToArray(),
        Conditions?.Select(x => new CouponConditionInput(x.Type, x.Value)).ToArray(),
        CanStackWithPromotions,
        CanStackWithCoupons);
}

public sealed record CouponBulkGenerateResponse(
    Guid BatchId,
    Guid? CampaignId,
    int CreatedCount,
    IReadOnlyCollection<string> Codes);

public sealed record CouponBulkGenerationQueuedResponse(
    Guid JobId,
    Guid BatchId,
    string BackgroundJobId,
    string Status,
    int RequestedCount);

public sealed record CouponBulkGenerationJobResponse(
    Guid Id,
    Guid BatchId,
    Guid? CampaignId,
    string Prefix,
    int RequestedCount,
    int CreatedCount,
    string Status,
    string? BackgroundJobId,
    string? Error,
    Guid? RequestedByUserId,
    string? RequestedByUserType,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc)
{
    public static CouponBulkGenerationJobResponse From(CouponBulkGenerationJob job)
        => new(
            job.Id,
            job.BatchId,
            job.CampaignId,
            job.Prefix,
            job.RequestedCount,
            job.CreatedCount,
            job.Status,
            job.BackgroundJobId,
            job.Error,
            job.RequestedByUserId,
            job.RequestedByUserType,
            job.RequestedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);
}

public sealed record CouponRequest(
    string Code,
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
    IReadOnlyCollection<CouponScopeRequest>? Scopes = null,
    IReadOnlyCollection<CouponCustomerScopeRequest>? CustomerScopes = null,
    IReadOnlyCollection<CouponConditionRequest>? Conditions = null,
    bool CanStackWithPromotions = true,
    bool CanStackWithCoupons = true,
    Guid? CampaignId = null)
{
    public CouponInput ToInput() => new(
        Code, Type, Value, MinimumSubtotal, MaximumDiscount,
        StartsAtUtc, EndsAtUtc, CanCombineWithFlashSale,
        MaximumTotalUses, MaximumUsesPerCustomer, IsActive,
        Scopes?.Select(x => new CouponScopeInput(
            x.Type, x.ProductId, x.VariantId, x.Sku, CategoryName: x.CategoryName)).ToArray(),
        CustomerScopes?.Select(x => new CouponCustomerScopeInput(x.Type, x.CustomerId)).ToArray(),
        Conditions?.Select(x => new CouponConditionInput(x.Type, x.Value)).ToArray(),
        CanStackWithPromotions,
        CanStackWithCoupons,
        CampaignId,
        Name,
        Description);
}

public sealed record CouponScopeRequest(
    string Type,
    Guid? ProductId,
    Guid? VariantId,
    string? Sku,
    string? CategoryName);

public sealed record CouponCustomerScopeRequest(
    string Type,
    Guid? CustomerId);

public sealed record CouponConditionRequest(string Type, string Value);

public sealed record CouponResponse(
    Guid Id,
    Guid? CampaignId,
    string Code,
    string Name,
    string? Description,
    string Type,
    decimal Value,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool CanCombineWithFlashSale,
    bool CanStackWithPromotions,
    bool CanStackWithCoupons,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    int UsedCount,
    bool IsActive,
    IReadOnlyCollection<CouponScopeResponse> Scopes,
    IReadOnlyCollection<CouponCustomerScopeResponse> CustomerScopes,
    IReadOnlyCollection<CouponConditionResponse> Conditions)
{
    public static CouponResponse From(Coupon x) => new(
        x.Id, x.CampaignId, x.Code, x.Name, x.Description,
        x.Type, x.Value, x.MinimumSubtotal, x.MaximumDiscount,
        x.StartsAtUtc, x.EndsAtUtc, x.CanCombineWithFlashSale,
        x.CanStackWithPromotions, x.CanStackWithCoupons,
        x.MaximumTotalUses, x.MaximumUsesPerCustomer, x.UsedCount, x.IsActive,
        x.Scopes.Select(s => new CouponScopeResponse(
            s.Id, s.Type, s.ProductId, s.VariantId, s.Sku, s.CategoryName)).ToArray(),
        x.CustomerScopes.Select(s => new CouponCustomerScopeResponse(s.Id, s.Type, s.CustomerId)).ToArray(),
        x.Conditions.Select(c => new CouponConditionResponse(c.Id, c.Type, c.Value)).ToArray());
}

public sealed record CouponScopeResponse(
    Guid Id,
    string Type,
    Guid? ProductId,
    Guid? VariantId,
    string? Sku,
    string? CategoryName);

public sealed record CouponCustomerScopeResponse(
    Guid Id,
    string Type,
    Guid? CustomerId);

public sealed record CouponConditionResponse(Guid Id, string Type, string Value);
