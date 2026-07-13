using System.Text.Json;

namespace PonPon.Modules.Ordering.Application.Pricing;

public sealed record PricingLineInput(
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice,
    int SellVatStatus,
    string? ImageUrl,
    string? OptionsJson,
    long? ZortCategoryId = null,
    string? CategoryName = null,
    long? ZortSubCategoryId = null,
    string? SubCategoryName = null);

public sealed class PricingLine
{
    public PricingLine(PricingLineInput input)
    {
        Input = input;
        UnitPrice = input.UnitPrice;
    }

    public PricingLineInput Input { get; }
    public decimal UnitPrice { get; set; }
    public decimal BaseTotal => Input.UnitPrice * Input.Quantity;
    public decimal Total => UnitPrice * Input.Quantity;
    public decimal DiscountAmount => BaseTotal - Total;
}

public sealed record PriceAdjustment(
    string Type,
    string Code,
    string Description,
    decimal Amount,
    Guid? ProductId = null);

public sealed record AppliedPromotion(
    Guid PromotionId,
    string Name,
    decimal DiscountAmount);

public sealed record AppliedCoupon(
    Guid CouponId,
    string Code,
    string Name,
    string Type,
    decimal DiscountAmount);

public sealed class PricingContext
{
    public PricingContext(
        IReadOnlyCollection<PricingLineInput> lines,
        decimal shippingAmount,
        string? couponCode,
        DateTime nowUtc,
        Guid? customerId = null,
        string? salesChannel = null,
        string? paymentMethod = null,
        string? shippingChannel = null,
        IReadOnlyCollection<string>? couponCodes = null)
    {
        Lines = lines.Select(x => new PricingLine(x)).ToList();
        ShippingAmount = shippingAmount;
        CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim();
        CouponCodes = NormalizeCouponCodes(couponCode, couponCodes);
        NowUtc = nowUtc;
        CustomerId = customerId;
        SalesChannel = Normalize(salesChannel);
        PaymentMethod = Normalize(paymentMethod);
        ShippingChannel = Normalize(shippingChannel);
    }

    public List<PricingLine> Lines { get; }
    public List<PriceAdjustment> Adjustments { get; } = [];
    public decimal ShippingAmount { get; }
    public string? CouponCode { get; }
    public IReadOnlyCollection<string> CouponCodes { get; }
    public DateTime NowUtc { get; }
    public Guid? CustomerId { get; }
    public string? SalesChannel { get; }
    public string? PaymentMethod { get; }
    public string? ShippingChannel { get; }
    public decimal CouponDiscountAmount { get; set; }
    public decimal CouponTaxableDiscountAmount { get; set; }
    public decimal CouponShippingDiscountAmount { get; set; }
    public Guid? AppliedCouponId { get; set; }
    public List<AppliedCoupon> AppliedCoupons { get; } = [];
    public Guid? AppliedFlashSaleId { get; set; }
    public List<AppliedPromotion> AppliedPromotions { get; } = [];
    public decimal PromotionDiscountAmount { get; set; }
    public decimal PromotionTaxableDiscountAmount { get; set; }
    public decimal PromotionShippingDiscountAmount { get; set; }
    public decimal ShippingDiscountAmount =>
        CouponShippingDiscountAmount + PromotionShippingDiscountAmount;
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public bool HasFlashSale => Adjustments.Any(x => x.Type == "flash_sale");
    public bool CanApplyCoupon { get; set; } = true;

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static IReadOnlyCollection<string> NormalizeCouponCodes(
        string? couponCode,
        IReadOnlyCollection<string>? couponCodes)
    {
        var codes = new List<string>();
        if (!string.IsNullOrWhiteSpace(couponCode))
            codes.Add(couponCode.Trim());
        if (couponCodes is not null)
            codes.AddRange(couponCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

        return codes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record PricedLine(
    PricingLineInput Input,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal Total);

public sealed record PricingResult(
    IReadOnlyCollection<PricedLine> Lines,
    decimal ItemSubtotal,
    decimal ShippingAmount,
    decimal ShippingDiscountAmount,
    decimal OrderDiscountAmount,
    decimal CouponDiscountAmount,
    decimal PromotionDiscountAmount,
    Guid? AppliedCouponId,
    IReadOnlyCollection<AppliedCoupon> AppliedCoupons,
    Guid? AppliedFlashSaleId,
    IReadOnlyCollection<AppliedPromotion> AppliedPromotions,
    decimal VatAmount,
    decimal GrandTotal,
    IReadOnlyCollection<PriceAdjustment> Adjustments,
    string SnapshotJson)
{
    public static PricingResult From(PricingContext context)
    {
        var lines = context.Lines
            .Select(x => new PricedLine(x.Input, x.UnitPrice, x.DiscountAmount, x.Total))
            .ToArray();
        var snapshot = new
        {
            Version = 1,
            context.NowUtc,
            context.SalesChannel,
            context.PaymentMethod,
            context.ShippingChannel,
            Lines = lines.Select(x => new
            {
                x.Input.ProductId,
                x.Input.VariantId,
                x.Input.Sku,
                x.Input.Quantity,
                BaseUnitPrice = x.Input.UnitPrice,
                x.UnitPrice,
                x.DiscountAmount,
                x.Total,
                x.Input.SellVatStatus,
                x.Input.ZortCategoryId,
                x.Input.CategoryName,
                x.Input.ZortSubCategoryId,
                x.Input.SubCategoryName
            }),
            ItemSubtotal = lines.Sum(x => x.Total),
            context.ShippingAmount,
            context.ShippingDiscountAmount,
            OrderDiscountAmount = context.CouponDiscountAmount + context.PromotionDiscountAmount,
            PromotionDiscountAmount = context.PromotionDiscountAmount,
            TotalDiscountAmount = context.CouponDiscountAmount + context.PromotionDiscountAmount,
            CouponTaxableDiscountAmount = context.CouponTaxableDiscountAmount,
            PromotionTaxableDiscountAmount = context.PromotionTaxableDiscountAmount,
            context.CouponShippingDiscountAmount,
            context.PromotionShippingDiscountAmount,
            context.AppliedCouponId,
            context.AppliedCoupons,
            context.AppliedFlashSaleId,
            context.AppliedPromotions,
            context.VatAmount,
            context.GrandTotal,
            context.Adjustments
        };

        return new PricingResult(
            lines,
            lines.Sum(x => x.Total),
            context.ShippingAmount,
            context.ShippingDiscountAmount,
            context.CouponDiscountAmount + context.PromotionDiscountAmount,
            context.CouponDiscountAmount,
            context.PromotionDiscountAmount,
            context.AppliedCouponId,
            context.AppliedCoupons.ToArray(),
            context.AppliedFlashSaleId,
            context.AppliedPromotions.ToArray(),
            context.VatAmount,
            context.GrandTotal,
            context.Adjustments.ToArray(),
            JsonSerializer.Serialize(snapshot));
    }
}
