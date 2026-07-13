using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Domain;
using PonPon.Modules.Promotion.Infrastructure;
using PonPon.Shared.Application.Exceptions;
using PromotionEntity = PonPon.Modules.Promotion.Domain.Promotion;

namespace PonPon.Modules.Promotion.Application;

public sealed class PromotionService(PromotionDbContext db) : IPromotionService
{
    private static readonly HashSet<string> SupportedPaymentMethods =
    [
        "promptpay",
        "card",
        "mobile_banking_bbl",
        "mobile_banking_kbank",
        "mobile_banking_scb",
        "mobile_banking_ktb",
        "mobile_banking_bay"
    ];

    public async Task<IReadOnlyCollection<PromotionEntity>> GetAllAsync(
        Guid? campaignId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Promotions.AsNoTracking();
        if (campaignId.HasValue)
            query = query.Where(x => x.CampaignId == campaignId.Value);

        return await query
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PromotionEntity>> GetActiveAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
        => await db.Promotions.AsNoTracking()
            .Where(x => x.IsActive
                && (!x.StartsAtUtc.HasValue || nowUtc >= x.StartsAtUtc.Value)
                && (!x.EndsAtUtc.HasValue || nowUtc <= x.EndsAtUtc.Value)
                && (!x.MaximumTotalUses.HasValue || x.UsedCount < x.MaximumTotalUses.Value)
                && (!x.CampaignId.HasValue
                    || db.CouponCampaigns.Any(c =>
                        c.Id == x.CampaignId.Value
                        && c.IsActive
                        && (!c.StartsAtUtc.HasValue || nowUtc >= c.StartsAtUtc.Value)
                        && (!c.EndsAtUtc.HasValue || nowUtc <= c.EndsAtUtc.Value))))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<PromotionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<PromotionUsage>> GetUsagesAsync(
        Guid promotionId,
        CancellationToken cancellationToken = default)
        => await db.PromotionUsages.AsNoTracking()
            .Where(x => x.PromotionId == promotionId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public async Task<Guid> CreateAsync(
        PromotionInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        await EnsureCampaignExistsAsync(input.CampaignId, cancellationToken);
        var promotion = PromotionEntity.Create(input, DateTime.UtcNow);
        await db.Promotions.AddAsync(promotion, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return promotion.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        PromotionInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        await EnsureCampaignExistsAsync(input.CampaignId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var affectedRows = await UpdatePromotionAsync(id, input, DateTime.UtcNow, cancellationToken);
        if (affectedRows == 0)
            throw new NotFoundException("Promotion was not found.");

        await DeletePromotionRulesAsync(id, cancellationToken);
        await AddPromotionRulesAsync(id, input, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await db.Promotions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Promotion was not found.");
        if (await db.PromotionUsages.AnyAsync(x => x.PromotionId == id, cancellationToken))
            promotion.Deactivate(DateTime.UtcNow);
        else
            db.Promotions.Remove(promotion);

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetActiveCustomerUsageCountAsync(
        Guid promotionId,
        Guid customerId,
        CancellationToken cancellationToken = default)
        => db.PromotionUsages.CountAsync(
            x => x.PromotionId == promotionId && x.CustomerId == customerId && !x.IsReleased,
            cancellationToken);

    public async Task<bool> TryReserveAsync(
        Guid promotionId,
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default,
        decimal discountAmount = 0)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var promotionGlobalKey = BitConverter.ToInt64(promotionId.ToByteArray(), 0);
        var promotionKey = BitConverter.ToInt32(promotionId.ToByteArray(), 0);
        var customerKey = BitConverter.ToInt32(customerId.ToByteArray(), 0);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({promotionGlobalKey})",
            cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({promotionKey}, {customerKey})",
            cancellationToken);

        var existing = await db.PromotionUsages
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.PromotionId == promotionId, cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existing.CustomerId == customerId && !existing.IsReleased;
        }

        var now = DateTime.UtcNow;
        var promotion = await db.Promotions.FirstOrDefaultAsync(
            x => x.Id == promotionId
                && (!x.CampaignId.HasValue
                    || db.CouponCampaigns.Any(c =>
                        c.Id == x.CampaignId.Value
                        && c.IsActive
                        && (!c.StartsAtUtc.HasValue || now >= c.StartsAtUtc.Value)
                        && (!c.EndsAtUtc.HasValue || now <= c.EndsAtUtc.Value))),
            cancellationToken);
        if (promotion is null
            || !promotion.IsActive
            || (promotion.StartsAtUtc.HasValue && now < promotion.StartsAtUtc.Value)
            || (promotion.EndsAtUtc.HasValue && now > promotion.EndsAtUtc.Value)
            || (promotion.MaximumTotalUses.HasValue && promotion.UsedCount >= promotion.MaximumTotalUses.Value))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (promotion.MaximumUsesPerCustomer.HasValue)
        {
            var customerUses = await db.PromotionUsages.CountAsync(
                x => x.PromotionId == promotionId && x.CustomerId == customerId && !x.IsReleased,
                cancellationToken);
            if (customerUses >= promotion.MaximumUsesPerCustomer.Value)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
        }

        promotion.IncrementUsage();
        await db.PromotionUsages.AddAsync(
            PromotionUsage.Reserve(promotionId, orderId, customerId, discountAmount, now),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var usages = await db.PromotionUsages
            .Where(x => x.OrderId == orderId && !x.IsReleased)
            .ToArrayAsync(cancellationToken);
        if (usages.Length == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var promotionIds = usages.Select(x => x.PromotionId).Distinct().ToArray();
        var promotions = await db.Promotions
            .Where(x => promotionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        foreach (var usage in usages)
        {
            usage.Release(DateTime.UtcNow);
            if (promotions.TryGetValue(usage.PromotionId, out var promotion))
                promotion.DecrementUsage();
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void Validate(PromotionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 256)
            throw new BadRequestException("Promotion name is required and must not exceed 256 characters.");
        if (input.Description?.Trim().Length > 2000)
            throw new BadRequestException("Promotion description must not exceed 2000 characters.");
        var type = input.Type.Trim().ToLowerInvariant();
        if (type is not ("auto_discount" or "flash_sale" or "free_shipping" or "special_price" or "bundle" or "buy_x_get_y"))
            throw new BadRequestException("Promotion type is invalid.");
        var discountType = input.DiscountType.Trim().ToLowerInvariant();
        if (discountType is not ("fixed" or "percentage" or "special_price" or "free_shipping"))
            throw new BadRequestException("Promotion discountType is invalid.");
        if (input.DiscountValue < 0 || (discountType == "percentage" && input.DiscountValue > 100))
            throw new BadRequestException("Promotion discount value is invalid.");
        if (discountType != "free_shipping" && input.DiscountValue <= 0)
            throw new BadRequestException("Promotion discount value must be greater than zero.");
        if (input.MinimumSubtotal < 0 || input.MaximumDiscount is <= 0)
            throw new BadRequestException("Promotion minimum subtotal or maximum discount is invalid.");
        if (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue && input.StartsAtUtc.Value >= input.EndsAtUtc.Value)
            throw new BadRequestException("Promotion start date must be earlier than end date.");
        if (input.MaximumTotalUses is <= 0 || input.MaximumUsesPerCustomer is <= 0)
            throw new BadRequestException("Promotion usage limits must be greater than zero.");

        foreach (var rule in input.ScheduleRules)
        {
            var ruleType = rule.Type.Trim().ToLowerInvariant();
            if (ruleType is not ("daily_time" or "day_of_week" or "day_of_month"))
                throw new BadRequestException("Promotion schedule rule type is invalid.");
            if (ruleType == "day_of_week" && rule.DayOfWeek is null or < 0 or > 6)
                throw new BadRequestException("Promotion day_of_week rule requires dayOfWeek 0-6.");
            if (ruleType == "day_of_month" && rule.DayOfMonth is null or < 1 or > 31)
                throw new BadRequestException("Promotion day_of_month rule requires dayOfMonth 1-31.");
            if (rule.StartsAtLocalTime.HasValue != rule.EndsAtLocalTime.HasValue)
                throw new BadRequestException("Promotion schedule local time range must have both start and end.");
            if (rule.StartsAtLocalTime.HasValue && rule.StartsAtLocalTime.Value >= rule.EndsAtLocalTime!.Value)
                throw new BadRequestException("Promotion schedule local start time must be earlier than end time.");
        }

        foreach (var scope in input.Scopes)
        {
            var scopeType = scope.Type.Trim().ToLowerInvariant();
            if (scopeType is not ("product" or "variant" or "sku" or "category"))
                throw new BadRequestException("Promotion scope type must be product, variant, sku, or category.");
            if (scopeType == "product" && scope.ProductId is null)
                throw new BadRequestException("Product promotion scope requires productId.");
            if (scopeType == "variant" && scope.VariantId is null && string.IsNullOrWhiteSpace(scope.Sku))
                throw new BadRequestException("Variant promotion scope requires variantId or sku.");
            if (scopeType == "sku" && string.IsNullOrWhiteSpace(scope.Sku))
                throw new BadRequestException("SKU promotion scope requires sku.");
            if (scopeType == "category" && string.IsNullOrWhiteSpace(scope.CategoryName))
                throw new BadRequestException("Category promotion scope requires categoryName.");
        }

        foreach (var scope in input.CustomerScopes)
        {
            var scopeType = scope.Type.Trim().ToLowerInvariant();
            if (scopeType is not ("new_customer" or "existing_customer" or "first_order" or "customer"))
                throw new BadRequestException("Promotion customer scope type is invalid.");
            if (scopeType == "customer" && scope.CustomerId is null)
                throw new BadRequestException("Customer promotion scope requires customerId.");
        }

        foreach (var condition in input.Conditions)
        {
            var conditionType = condition.Type.Trim().ToLowerInvariant();
            if (conditionType is not ("sales_channel" or "payment_method" or "shipping_channel"))
                throw new BadRequestException("Promotion condition type is invalid.");
            if (string.IsNullOrWhiteSpace(condition.Value) || condition.Value.Trim().Length > 128)
                throw new BadRequestException("Promotion condition value is required and must not exceed 128 characters.");
            if (conditionType == "payment_method"
                && !SupportedPaymentMethods.Contains(condition.Value.Trim().ToLowerInvariant()))
                throw new BadRequestException("Promotion payment method is not supported.");
        }
    }

    private async Task EnsureCampaignExistsAsync(Guid? campaignId, CancellationToken cancellationToken)
    {
        if (campaignId.HasValue
            && !await db.CouponCampaigns.AnyAsync(x => x.Id == campaignId.Value && x.IsActive, cancellationToken))
            throw new BadRequestException("Campaign was not found or is inactive.");
    }

    private async Task DeletePromotionRulesAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        await db.Set<PromotionScheduleRule>()
            .Where(x => x.PromotionId == promotionId)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Set<PromotionScope>()
            .Where(x => x.PromotionId == promotionId)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Set<PromotionCustomerScope>()
            .Where(x => x.PromotionId == promotionId)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Set<PromotionCondition>()
            .Where(x => x.PromotionId == promotionId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task<int> UpdatePromotionAsync(
        Guid promotionId,
        PromotionInput input,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var name = input.Name.Trim();
        var description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        var type = input.Type.Trim().ToLowerInvariant();
        var discountType = input.DiscountType.Trim().ToLowerInvariant();
        var timezone = string.IsNullOrWhiteSpace(input.Timezone) ? "Asia/Bangkok" : input.Timezone.Trim();

        return db.Promotions
            .Where(x => x.Id == promotionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CampaignId, input.CampaignId)
                .SetProperty(x => x.Name, name)
                .SetProperty(x => x.Description, description)
                .SetProperty(x => x.Type, type)
                .SetProperty(x => x.DiscountType, discountType)
                .SetProperty(x => x.DiscountValue, input.DiscountValue)
                .SetProperty(x => x.MinimumSubtotal, input.MinimumSubtotal)
                .SetProperty(x => x.MaximumDiscount, input.MaximumDiscount)
                .SetProperty(x => x.StartsAtUtc, input.StartsAtUtc)
                .SetProperty(x => x.EndsAtUtc, input.EndsAtUtc)
                .SetProperty(x => x.Timezone, timezone)
                .SetProperty(x => x.Priority, input.Priority)
                .SetProperty(x => x.CanStackWithCoupon, input.CanStackWithCoupon)
                .SetProperty(x => x.CanStackWithPromotions, input.CanStackWithPromotions)
                .SetProperty(x => x.CanCombineWithFlashSale, input.CanCombineWithFlashSale)
                .SetProperty(x => x.MaximumTotalUses, input.MaximumTotalUses)
                .SetProperty(x => x.MaximumUsesPerCustomer, input.MaximumUsesPerCustomer)
                .SetProperty(x => x.IsActive, input.IsActive)
                .SetProperty(x => x.UpdatedAtUtc, now),
                cancellationToken);
    }

    private async Task AddPromotionRulesAsync(
        Guid promotionId,
        PromotionInput input,
        CancellationToken cancellationToken)
    {
        await db.Set<PromotionScheduleRule>().AddRangeAsync(
            input.ScheduleRules.Select(x => PromotionScheduleRule.Create(promotionId, x)),
            cancellationToken);
        await db.Set<PromotionScope>().AddRangeAsync(
            input.Scopes.Select(x => PromotionScope.Create(promotionId, x)),
            cancellationToken);
        await db.Set<PromotionCustomerScope>().AddRangeAsync(
            input.CustomerScopes.Select(x => PromotionCustomerScope.Create(promotionId, x)),
            cancellationToken);
        await db.Set<PromotionCondition>().AddRangeAsync(
            input.Conditions.Select(x => PromotionCondition.Create(promotionId, x)),
            cancellationToken);
    }
}
