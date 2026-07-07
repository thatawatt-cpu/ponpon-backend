using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;
using PonPon.Shared.Application.Exceptions;
using PromotionEntity = PonPon.Modules.Promotion.Domain.Promotion;

namespace PonPon.Modules.Ordering.Application.Pricing;

public sealed class AutoPromotionPricingStep : IPricingStep
{
    private readonly IPromotionService _promotions;
    private readonly IOrderRepository? _orders;

    public AutoPromotionPricingStep(IPromotionService promotions, IOrderRepository? orders = null)
    {
        _promotions = promotions;
        _orders = orders;
    }

    public int Order => 150;

    public async Task ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
    {
        var promotions = await _promotions.GetActiveAsync(context.NowUtc, cancellationToken);
        foreach (var promotion in promotions)
        {
            if (context.HasFlashSale && !promotion.CanCombineWithFlashSale)
                continue;
            if (promotion.ScheduleRules.Count > 0 && !MatchesAnySchedule(promotion, context.NowUtc))
                continue;
            if (!MatchesConditions(promotion, context))
                continue;
            if (!await IsCustomerEligibleAsync(promotion, context, cancellationToken))
                continue;
            if (context.CustomerId.HasValue && promotion.MaximumUsesPerCustomer.HasValue
                && await _promotions.GetActiveCustomerUsageCountAsync(
                    promotion.Id, context.CustomerId.Value, cancellationToken) >= promotion.MaximumUsesPerCustomer.Value)
                continue;

            var eligibleLines = GetEligibleLines(promotion, context);
            if (eligibleLines.Count == 0)
                continue;
            var eligibleSubtotal = eligibleLines.Sum(x => x.Total);
            if (eligibleSubtotal < promotion.MinimumSubtotal)
                continue;

            var availableShippingAmount = Math.Max(0, context.ShippingAmount - context.ShippingDiscountAmount);
            var discount = CalculateDiscount(promotion, eligibleSubtotal, availableShippingAmount);
            if (discount <= 0)
                continue;

            var isFreeShipping = promotion.DiscountType == "free_shipping";
            var taxableDiscount = isFreeShipping
                ? 0
                : CalculateTaxableDiscount(eligibleLines, eligibleSubtotal, discount);
            context.PromotionDiscountAmount += discount;
            context.PromotionTaxableDiscountAmount += taxableDiscount;
            if (isFreeShipping)
                context.PromotionShippingDiscountAmount += discount;
            context.AppliedPromotions.Add(new AppliedPromotion(promotion.Id, promotion.Name, discount));
            context.Adjustments.Add(new PriceAdjustment(
                "promotion",
                promotion.Id.ToString("N"),
                $"Promotion {promotion.Name}",
                discount));
            if (!promotion.CanStackWithCoupon)
                context.CanApplyCoupon = false;

            if (!promotion.CanStackWithPromotions)
                break;
        }
    }

    private async Task<bool> IsCustomerEligibleAsync(
        PromotionEntity promotion,
        PricingContext context,
        CancellationToken cancellationToken)
    {
        if (promotion.CustomerScopes.Count == 0)
            return true;
        if (context.CustomerId is not Guid customerId)
            return false;

        int? completedOrderCount = null;
        foreach (var scope in promotion.CustomerScopes)
        {
            switch (scope.Type)
            {
                case "customer" when scope.CustomerId == customerId:
                    return true;
                case "new_customer":
                case "first_order":
                    completedOrderCount ??= await CountCompletedOrdersAsync(customerId, cancellationToken);
                    if (completedOrderCount == 0)
                        return true;
                    break;
                case "existing_customer":
                    completedOrderCount ??= await CountCompletedOrdersAsync(customerId, cancellationToken);
                    if (completedOrderCount > 0)
                        return true;
                    break;
            }
        }

        return false;
    }

    private async Task<int> CountCompletedOrdersAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (_orders is null)
            throw new BadRequestException("Promotion customer eligibility cannot be checked.");
        return await _orders.CountCustomerCompletedOrdersAsync(customerId, cancellationToken);
    }

    private static bool MatchesAnySchedule(PromotionEntity promotion, DateTime nowUtc)
    {
        var localNow = ToLocalNow(promotion.Timezone, nowUtc);
        return promotion.ScheduleRules.Any(rule =>
        {
            if (rule.Type == "day_of_week" && rule.DayOfWeek != (int)localNow.DayOfWeek)
                return false;
            if (rule.Type == "day_of_month" && rule.DayOfMonth != localNow.Day)
                return false;
            if (rule.Type == "daily_time" || rule.StartsAtLocalTime.HasValue)
            {
                var time = TimeOnly.FromDateTime(localNow);
                if (!rule.StartsAtLocalTime.HasValue || !rule.EndsAtLocalTime.HasValue)
                    return true;
                return time >= rule.StartsAtLocalTime.Value && time <= rule.EndsAtLocalTime.Value;
            }

            return true;
        });
    }

    private static DateTime ToLocalNow(string timezone, DateTime nowUtc)
    {
        var zone = ResolveTimeZone(timezone);
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            zone);
    }

    private static TimeZoneInfo ResolveTimeZone(string timezone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException) when (timezone == "Asia/Bangkok")
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static bool MatchesConditions(PromotionEntity promotion, PricingContext context)
    {
        foreach (var group in promotion.Conditions.GroupBy(x => x.Type))
        {
            var actual = group.Key switch
            {
                "sales_channel" => context.SalesChannel,
                "payment_method" => context.PaymentMethod,
                "shipping_channel" => context.ShippingChannel,
                _ => null
            };
            if (actual is null || !group.Any(x => string.Equals(x.Value, actual, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    private static List<PricingLine> GetEligibleLines(PromotionEntity promotion, PricingContext context)
    {
        var includeScopes = promotion.Scopes.Where(x => !x.IsExclude).ToArray();
        var excludeScopes = promotion.Scopes.Where(x => x.IsExclude).ToArray();
        var lines = includeScopes.Length == 0
            ? context.Lines
            : context.Lines.Where(line => includeScopes.Any(scope => Matches(scope, line))).ToList();

        if (excludeScopes.Length > 0)
            lines = lines.Where(line => !excludeScopes.Any(scope => Matches(scope, line))).ToList();

        return lines;
    }

    private static decimal CalculateDiscount(PromotionEntity promotion, decimal eligibleSubtotal, decimal shippingAmount)
    {
        var discount = promotion.DiscountType switch
        {
            "fixed" => promotion.DiscountValue,
            "percentage" => eligibleSubtotal * promotion.DiscountValue / 100m,
            "special_price" => Math.Max(0, eligibleSubtotal - promotion.DiscountValue),
            "free_shipping" => shippingAmount,
            _ => 0
        };
        if (promotion.MaximumDiscount.HasValue)
            discount = Math.Min(discount, promotion.MaximumDiscount.Value);

        return decimal.Round(
            Math.Clamp(discount, 0, eligibleSubtotal + shippingAmount),
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateTaxableDiscount(
        IReadOnlyCollection<PricingLine> eligibleLines,
        decimal eligibleSubtotal,
        decimal discount)
    {
        var eligibleTaxableSubtotal = eligibleLines
            .Where(x => x.Input.SellVatStatus > 0)
            .Sum(x => x.Total);
        var taxableDiscount = eligibleSubtotal <= 0
            ? 0
            : discount * eligibleTaxableSubtotal / eligibleSubtotal;
        return decimal.Round(
            Math.Clamp(taxableDiscount, 0, discount),
            2,
            MidpointRounding.AwayFromZero);
    }

    private static bool Matches(PromotionScope scope, PricingLine line)
    {
        return scope.Type switch
        {
            "product" => scope.ProductId == line.Input.ProductId,
            "variant" => scope.VariantId == line.Input.VariantId || SkuEquals(scope.Sku, line.Input.Sku),
            "sku" => SkuEquals(scope.Sku, line.Input.Sku),
            "category" => CategoryEquals(scope.CategoryName, line.Input.CategoryName)
                          || CategoryEquals(scope.CategoryName, line.Input.SubCategoryName),
            _ => false
        };
    }

    private static bool SkuEquals(string? left, string right)
        => !string.IsNullOrWhiteSpace(left)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool CategoryEquals(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
