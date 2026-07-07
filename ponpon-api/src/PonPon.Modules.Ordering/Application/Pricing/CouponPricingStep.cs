using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Pricing;

public sealed class CouponPricingStep : IPricingStep
{
    private readonly ICouponService _coupons;
    private readonly IOrderRepository? _orders;

    public CouponPricingStep(ICouponService coupons, IOrderRepository? orders = null)
    {
        _coupons = coupons;
        _orders = orders;
    }

    public int Order => 200;

    public async Task ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
    {
        if (context.CouponCodes.Count == 0)
            return;
        if (!context.CanApplyCoupon)
            throw new BadRequestException("Coupon cannot be combined with an active promotion.");
        if (context.CouponCodes.Count > 2)
            throw new BadRequestException("Only one discount coupon and one free shipping coupon can be used together.");

        var coupons = new List<Coupon>(context.CouponCodes.Count);
        foreach (var code in context.CouponCodes)
        {
            var coupon = await _coupons.GetByCodeAsync(code, cancellationToken)
                ?? throw new BadRequestException("Coupon code is invalid.");
            coupons.Add(coupon);
        }

        if (coupons.Select(x => x.Id).Distinct().Count() != coupons.Count)
            throw new BadRequestException("Duplicate coupon code is not allowed.");
        if (coupons.Count > 1 && coupons.Any(x => !x.CanStackWithCoupons))
            throw new BadRequestException("One or more coupons cannot be combined with another coupon.");
        if (coupons.Count(x => x.Type == "free_shipping") > 1)
            throw new BadRequestException("Only one free shipping coupon can be used.");
        if (coupons.Count(x => x.Type != "free_shipping") > 1)
            throw new BadRequestException("Only one discount coupon can be used.");

        foreach (var coupon in coupons.OrderBy(x => x.Type == "free_shipping" ? 1 : 0))
        {
            await ApplyCouponAsync(coupon, context, cancellationToken);
        }
    }

    private async Task ApplyCouponAsync(
        Coupon coupon,
        PricingContext context,
        CancellationToken cancellationToken)
    {
        if (!coupon.IsActive
            || (coupon.StartsAtUtc.HasValue && context.NowUtc < coupon.StartsAtUtc.Value)
            || (coupon.EndsAtUtc.HasValue && context.NowUtc > coupon.EndsAtUtc.Value)
            || (coupon.MaximumTotalUses.HasValue && coupon.UsedCount >= coupon.MaximumTotalUses.Value))
        {
            throw new BadRequestException("Coupon is not active or its quota is exhausted.");
        }
        if (context.CustomerId.HasValue && coupon.MaximumUsesPerCustomer.HasValue
            && await _coupons.GetActiveCustomerUsageCountAsync(
                coupon.Id, context.CustomerId.Value, cancellationToken) >= coupon.MaximumUsesPerCustomer.Value)
            throw new BadRequestException("Coupon usage limit for this customer has been reached.");
        if (context.HasFlashSale && !coupon.CanCombineWithFlashSale)
            throw new BadRequestException("Coupon cannot be combined with a flash sale.");
        if (context.AppliedPromotions.Count > 0 && !coupon.CanStackWithPromotions)
            throw new BadRequestException("Coupon cannot be combined with active promotions.");
        if (!await IsCustomerEligibleAsync(coupon, context, cancellationToken))
            throw new BadRequestException("Coupon is not applicable to this customer.");
        ValidateConditions(coupon, context);

        var eligibleLines = coupon.Scopes.Count == 0
            ? context.Lines
            : context.Lines.Where(line => coupon.Scopes.Any(scope => Matches(scope, line))).ToList();
        if (eligibleLines.Count == 0)
            throw new BadRequestException("Coupon is not applicable to the selected products.");

        var eligibleSubtotal = eligibleLines.Sum(x => x.Total);
        if (eligibleSubtotal < coupon.MinimumSubtotal)
            throw new BadRequestException($"Coupon requires a minimum subtotal of {coupon.MinimumSubtotal:0.00}.");

        var availableShippingAmount = Math.Max(0, context.ShippingAmount - context.ShippingDiscountAmount);
        var isFreeShipping = coupon.Type == "free_shipping";
        var discount = coupon.Type switch
        {
            "fixed" => coupon.Value,
            "percentage" => eligibleSubtotal * coupon.Value / 100m,
            "free_shipping" => availableShippingAmount,
            _ => throw new BadRequestException("Coupon has an invalid discount type.")
        };
        if (coupon.MaximumDiscount.HasValue)
            discount = Math.Min(discount, coupon.MaximumDiscount.Value);

        discount = decimal.Round(
            Math.Clamp(discount, 0, isFreeShipping ? availableShippingAmount : eligibleSubtotal),
            2,
            MidpointRounding.AwayFromZero);
        var eligibleTaxableSubtotal = eligibleLines
            .Where(x => x.Input.SellVatStatus > 0)
            .Sum(x => x.Total);
        var taxableDiscount = isFreeShipping || eligibleSubtotal <= 0
            ? 0
            : discount * eligibleTaxableSubtotal / eligibleSubtotal;
        context.AppliedCouponId ??= coupon.Id;
        context.AppliedCoupons.Add(new AppliedCoupon(coupon.Id, coupon.Code, coupon.Type, discount));
        context.CouponDiscountAmount += discount;
        if (isFreeShipping)
            context.CouponShippingDiscountAmount += discount;
        context.CouponTaxableDiscountAmount += decimal.Round(
            Math.Clamp(taxableDiscount, 0, discount),
            2,
            MidpointRounding.AwayFromZero);
        context.Adjustments.Add(new PriceAdjustment("coupon", coupon.Code, $"Coupon {coupon.Code}", discount));
    }

    private async Task<bool> IsCustomerEligibleAsync(Coupon coupon, PricingContext context, CancellationToken cancellationToken)
    {
        if (coupon.CustomerScopes.Count == 0)
            return true;
        if (context.CustomerId is not Guid customerId)
            return false;

        int? completedOrderCount = null;
        foreach (var scope in coupon.CustomerScopes)
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
            throw new BadRequestException("Coupon customer eligibility cannot be checked.");
        return await _orders.CountCustomerCompletedOrdersAsync(customerId, cancellationToken);
    }

    private static void ValidateConditions(Coupon coupon, PricingContext context)
    {
        foreach (var group in coupon.Conditions.GroupBy(x => x.Type))
        {
            var actual = group.Key switch
            {
                "sales_channel" => context.SalesChannel,
                "payment_method" => context.PaymentMethod,
                "shipping_channel" => context.ShippingChannel,
                _ => null
            };
            if (actual is null || !group.Any(x =>
                    string.Equals(x.Value, actual, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException(
                    $"Coupon is not applicable to the selected {group.Key.Replace('_', ' ')}.");
            }
        }
    }

    private static bool Matches(CouponScope scope, PricingLine line)
    {
        return scope.Type switch
        {
            "product" => scope.ProductId == line.Input.ProductId,
            "variant" => scope.VariantId == line.Input.VariantId || SkuEquals(scope.Sku, line.Input.Sku),
            "sku" => SkuEquals(scope.Sku, line.Input.Sku),
            "category" =>
                (scope.ZortCategoryId.HasValue
                    && (scope.ZortCategoryId == line.Input.ZortCategoryId
                        || scope.ZortCategoryId == line.Input.ZortSubCategoryId))
                || CategoryEquals(scope.CategoryName, line.Input.CategoryName)
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
