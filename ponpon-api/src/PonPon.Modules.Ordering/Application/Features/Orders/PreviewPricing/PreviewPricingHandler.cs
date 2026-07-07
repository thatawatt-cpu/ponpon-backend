using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.PreviewPricing;

public sealed class PreviewPricingHandler(
    IProductRepository products,
    IShippingRateQuoteService shippingRates,
    PricingPipeline pipeline,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
{
    public async Task<PreviewPricingResponse> HandleAsync(
        PreviewPricingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0 || request.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
            throw new BadRequestException("At least one valid order item is required.");

        var lines = new List<PricingLineInput>();
        decimal weight = 0, width = 0, length = 0, height = 0;
        foreach (var item in request.Items
                     .GroupBy(x => new { x.ProductId, x.VariantId })
                     .Select(x => new PreviewPricingItemRequest(x.Key.ProductId, x.Key.VariantId, x.Sum(y => y.Quantity))))
        {
            var product = await products.GetByIdWithVariantsAsync(item.ProductId, cancellationToken)
                ?? throw new BadRequestException($"Product {item.ProductId} was not found.");
            if (!product.IsVisibleToCustomer)
                throw new BadRequestException($"Product {product.Name} is not available.");

            var variant = ResolveVariant(product, item.VariantId);
            if (item.Quantity > variant.AvailableStock)
                throw new BadRequestException($"Product {product.Name} ({variant.Sku}) has insufficient stock.");
            if (product.Weight is null or <= 0 || product.Width is null or <= 0
                || product.Length is null or <= 0 || product.Height is null or <= 0)
                throw new BadRequestException($"Product {product.Name} is missing weight or parcel dimensions.");

            weight += product.Weight.Value * item.Quantity;
            width = Math.Max(width, product.Width.Value);
            length = Math.Max(length, product.Length.Value);
            height = Math.Max(height, product.Height.Value);
            lines.Add(new PricingLineInput(product.Id, variant.Id, variant.Sku, product.Name,
                item.Quantity, variant.SellPrice, variant.SellVatStatus, variant.ImageUrl, variant.OptionsJson,
                product.ZortCategoryId, product.CategoryName, product.ZortSubCategoryId, product.SubCategoryName));
        }

        var address = ParseAddress(request.ShippingAddress)
            ?? throw new BadRequestException("ShippingAddress must end with district, state, province, and postcode.");
        if (string.IsNullOrWhiteSpace(request.ShippingChannel))
            throw new BadRequestException("ShippingChannel is required.");

        var shipping = await shippingRates.GetShippingAmountAsync(
            new ShippingRateQuoteRequest(
                request.ShippingName.Trim(), request.ShippingPhone.Trim(),
                string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
                address.Address, address.District, address.State, address.Province, address.Postcode,
                "Checkout pricing preview", (double)(weight / 1000m), (double)width, (double)length,
                (double)height, request.ShippingChannel.Trim()),
            cancellationToken);
        var result = await pipeline.ExecuteAsync(
            new PricingContext(
                lines,
                shipping,
                request.CouponCode,
                clock.UtcNow,
                currentUser.CustomerId,
                "LineLiff",
                request.PaymentMethod,
                request.ShippingChannel,
                request.CouponCodes),
            cancellationToken);

        return new PreviewPricingResponse(
            result.Lines.Select(x => new PreviewPricingLineResponse(
                x.Input.ProductId, x.Input.VariantId, x.Input.Sku, x.Input.Name, x.Input.Quantity,
                x.Input.UnitPrice, x.UnitPrice, x.DiscountAmount, x.Total)).ToArray(),
            result.ItemSubtotal, result.ShippingAmount, result.ShippingDiscountAmount,
            result.CouponDiscountAmount, result.PromotionDiscountAmount,
            result.OrderDiscountAmount, result.VatAmount,
            result.GrandTotal, result.Adjustments, result.AppliedCoupons);
    }

    private static ProductVariant ResolveVariant(Product product, Guid? variantId)
    {
        if (variantId is Guid id)
            return product.FindVariant(id)
                ?? throw new BadRequestException($"Variant {id} was not found for product {product.Id}.");
        var visible = product.Variants.Where(x => x.IsVisibleToCustomer).ToArray();
        return visible.Length switch
        {
            1 => visible[0],
            0 => throw new BadRequestException($"Product {product.Name} has no available variants."),
            _ => throw new BadRequestException($"Product {product.Name} requires a variantId.")
        };
    }

    private static ParsedAddress? ParseAddress(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length < 5 ? null : new(string.Join(' ', parts[..^4]), parts[^4], parts[^3], parts[^2], parts[^1]);
    }

    private sealed record ParsedAddress(string Address, string District, string State, string Province, string Postcode);
}

public sealed record PreviewPricingRequest(
    string? CustomerEmail,
    string ShippingName,
    string ShippingPhone,
    string ShippingAddress,
    string ShippingChannel,
    string? CouponCode,
    IReadOnlyCollection<PreviewPricingItemRequest> Items,
    string? PaymentMethod = null,
    IReadOnlyCollection<string>? CouponCodes = null);

public sealed record PreviewPricingItemRequest(Guid ProductId, Guid? VariantId, int Quantity);

public sealed record PreviewPricingResponse(
    IReadOnlyCollection<PreviewPricingLineResponse> Lines,
    decimal ItemSubtotal,
    decimal ShippingAmount,
    decimal ShippingDiscountAmount,
    decimal CouponDiscountAmount,
    decimal PromotionDiscountAmount,
    decimal OrderDiscountAmount,
    decimal VatAmount,
    decimal GrandTotal,
    IReadOnlyCollection<PriceAdjustment> Adjustments,
    IReadOnlyCollection<AppliedCoupon> AppliedCoupons);

public sealed record PreviewPricingLineResponse(
    Guid ProductId, Guid VariantId, string Sku, string Name, int Quantity,
    decimal BaseUnitPrice, decimal UnitPrice, decimal DiscountAmount, decimal Total);
