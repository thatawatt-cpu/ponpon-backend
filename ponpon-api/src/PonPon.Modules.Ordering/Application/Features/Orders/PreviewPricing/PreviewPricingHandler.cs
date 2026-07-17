using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.CheckoutPricing;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Modules.Ordering.Domain.Quotes;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.PreviewPricing;

public sealed class PreviewPricingHandler(
    CheckoutPricingQuoteService quoteService,
    ICheckoutQuoteRepository quotes,
    IOrderingUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
{
    public async Task<PreviewPricingResponse> HandleAsync(
        PreviewPricingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0 || request.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
            throw new BadRequestException("At least one valid order item is required.");

        var draft = await quoteService.CalculateAsync(
            new CheckoutPricingPayload(
                request.CustomerEmail,
                request.ShippingName,
                request.ShippingPhone,
                request.ShippingAddress,
                request.ShippingChannel,
                request.CouponCode,
                request.Items.Select(x => new CheckoutPricingItem(x.ProductId, x.VariantId, x.Quantity)).ToArray(),
                request.CouponCodes),
            cancellationToken);

        var now = clock.UtcNow;
        var quote = CheckoutQuote.Create(
            currentUser.CustomerId ?? throw new UnauthorizedException("Customer authentication is required."),
            draft.PayloadHash,
            draft.CalculationHash,
            draft.Pricing.SnapshotJson,
            now.AddMinutes(10),
            draft.IsFinal,
            draft.CalculationStatus,
            draft.ShippingFinalized,
            now);
        await quotes.AddAsync(quote, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PreviewPricingResponse(
            quote.Id,
            quote.ExpiresAtUtc,
            quote.IsFinal,
            quote.CalculationStatus,
            quote.ShippingFinalized,
            draft.ShippingChannel,
            draft.Packages,
            draft.Pricing.Lines.Select(x => new PreviewPricingLineResponse(
                x.Input.ProductId, x.Input.VariantId, x.Input.Sku, x.Input.Name, x.Input.Quantity,
                x.Input.UnitPrice, x.UnitPrice, x.DiscountAmount, x.Total)).ToArray(),
            draft.Pricing.ItemSubtotal, draft.Pricing.ShippingAmount, draft.Pricing.ShippingDiscountAmount,
            draft.Pricing.CouponDiscountAmount, draft.Pricing.PromotionDiscountAmount,
            draft.Pricing.OrderDiscountAmount, draft.Pricing.VatAmount,
            draft.Pricing.GrandTotal, draft.Pricing.Adjustments, draft.Pricing.AppliedCoupons);
    }
}

public sealed record PreviewPricingRequest(
    string? CustomerEmail,
    string? ShippingName,
    string? ShippingPhone,
    string? ShippingAddress,
    string? ShippingChannel,
    string? CouponCode,
    IReadOnlyCollection<PreviewPricingItemRequest> Items,
    IReadOnlyCollection<string>? CouponCodes = null);

public sealed record PreviewPricingItemRequest(Guid ProductId, Guid? VariantId, int Quantity);

public sealed record PreviewPricingResponse(
    Guid QuoteId,
    DateTime ExpiresAt,
    bool IsFinal,
    string CalculationStatus,
    bool ShippingFinalized,
    string? SelectedShippingChannel,
    IReadOnlyCollection<CheckoutShippingPackage> Packages,
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
