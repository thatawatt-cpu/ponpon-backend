namespace PonPon.Modules.Ordering.Application.Pricing;

public sealed class PricingOptions
{
    public bool VatEnabled { get; set; }
    public decimal VatRate { get; set; } = 0.07m;
    public bool PricesIncludeVat { get; set; } = true;
    public bool ShippingVatApplicable { get; set; } = true;
}
