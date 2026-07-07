using Microsoft.Extensions.Options;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Pricing;

public sealed class VatPricingStep : IPricingStep
{
    private readonly PricingOptions _options;

    public VatPricingStep(IOptions<PricingOptions> options)
    {
        _options = options.Value;
    }

    public int Order => 300;

    public Task ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
    {
        if (!_options.VatEnabled || _options.VatRate <= 0)
            return Task.CompletedTask;

        var taxableItemTotal = context.Lines
            .Where(x => x.Input.SellVatStatus > 0)
            .Sum(x => x.Total);
        var taxableShippingAmount = Math.Max(0, context.ShippingAmount - context.ShippingDiscountAmount);
        var taxableBase = Math.Max(
                0,
                taxableItemTotal - context.PromotionTaxableDiscountAmount - context.CouponTaxableDiscountAmount)
            + (_options.ShippingVatApplicable ? taxableShippingAmount : 0);

        context.VatAmount = decimal.Round(
            _options.PricesIncludeVat
                ? taxableBase * _options.VatRate / (1 + _options.VatRate)
                : taxableBase * _options.VatRate,
            2,
            MidpointRounding.AwayFromZero);
        return Task.CompletedTask;
    }
}

public sealed class FinalizePricingStep : IPricingStep
{
    private readonly PricingOptions _options;

    public FinalizePricingStep(IOptions<PricingOptions> options)
    {
        _options = options.Value;
    }

    public int Order => 400;

    public Task ExecuteAsync(PricingContext context, CancellationToken cancellationToken)
    {
        var total = context.Lines.Sum(x => x.Total)
            - context.PromotionDiscountAmount
            - context.CouponDiscountAmount
            + context.ShippingAmount;

        if (_options.VatEnabled && !_options.PricesIncludeVat)
            total += context.VatAmount;
        context.GrandTotal = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        if (context.GrandTotal < 0)
            throw new BadRequestException("Calculated order total cannot be negative.");

        return Task.CompletedTask;
    }
}
