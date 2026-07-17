using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.CheckoutPricing;

public sealed class CheckoutPricingQuoteService(
    IProductRepository products,
    IShippingRateQuoteService shippingRates,
    PricingPipeline pipeline,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CheckoutPricingPayloadHash> CalculatePayloadHashAsync(
        CheckoutPricingPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.Items.Count == 0 || payload.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
            throw new BadRequestException("At least one valid order item is required.");

        var resolved = await ResolvePayloadAsync(payload, cancellationToken);
        return new CheckoutPricingPayloadHash(
            Hash(NormalizePayload(payload, resolved.Lines, resolved.ShippingChannel, resolved.Packages)),
            resolved.ShippingFinalized,
            resolved.IsFinal,
            resolved.CalculationStatus);
    }

    public async Task<CheckoutPricingQuoteDraft> CalculateAsync(
        CheckoutPricingPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.Items.Count == 0 || payload.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
            throw new BadRequestException("At least one valid order item is required.");

        var resolved = await ResolvePayloadAsync(payload, cancellationToken);
        var shippingAmount = 0m;
        if (resolved.ShippingFinalized)
        {
            var shippingChannel = resolved.ShippingChannel!;
            var address = ParseAddress(payload.ShippingAddress!)
                ?? throw new BadRequestException("ShippingAddress must end with district, state, province, and postcode.");
            var shippingTasks = resolved.Packages
                .Select(package => shippingRates.GetShippingAmountAsync(
                    new ShippingRateQuoteRequest(
                        payload.ShippingName!.Trim(), payload.ShippingPhone!.Trim(),
                        EmptyToNull(payload.CustomerEmail),
                        address.Address, address.District, address.State, address.Province, address.Postcode,
                        $"Checkout pricing quote {package.BoxCode}",
                        (double)package.WeightKg, (double)package.WidthCm, (double)package.LengthCm,
                        (double)package.HeightCm, shippingChannel),
                    cancellationToken))
                .ToArray();
            shippingAmount = (await Task.WhenAll(shippingTasks)).Sum();
        }

        var result = await pipeline.ExecuteAsync(
            new PricingContext(
                resolved.Lines,
                shippingAmount,
                payload.CouponCode,
                clock.UtcNow,
                currentUser.CustomerId,
                SyncOrdersFromZortHandler.LineLiffSalesChannel,
                shippingChannel: resolved.ShippingChannel,
                couponCodes: payload.CouponCodes),
            cancellationToken);

        var payloadHash = Hash(NormalizePayload(payload, resolved.Lines, resolved.ShippingChannel, resolved.Packages));
        var calculationHash = Hash(NormalizeCalculation(payloadHash, resolved.ShippingFinalized, result, resolved.Packages));
        return new CheckoutPricingQuoteDraft(
            result,
            payloadHash,
            calculationHash,
            resolved.ShippingFinalized,
            IsFinal: resolved.IsFinal,
            CalculationStatus: resolved.CalculationStatus,
            resolved.ShippingChannel,
            resolved.Packages);
    }

    private async Task<ResolvedCheckoutPricingPayload> ResolvePayloadAsync(
        CheckoutPricingPayload payload,
        CancellationToken cancellationToken)
    {
        var requestedItems = payload.Items
            .GroupBy(x => new { x.ProductId, x.VariantId })
            .Select(x => new CheckoutPricingItem(x.Key.ProductId, x.Key.VariantId, x.Sum(y => y.Quantity)))
            .ToArray();
        var productIds = requestedItems.Select(x => x.ProductId).ToHashSet();
        var productMap = (await products.GetByIdsWithVariantsAsync(productIds, cancellationToken))
            .ToDictionary(x => x.Id);

        var lines = new List<PricingLineInput>();
        var packableItems = new List<PackableItem>();
        foreach (var item in requestedItems)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new BadRequestException($"Product {item.ProductId} was not found.");

            if (!product.IsVisibleToCustomer)
                throw new BadRequestException($"Product {product.Name} is not available.");

            var variant = ResolveVariant(product, item.VariantId);
            if (item.Quantity > variant.AvailableStock)
                throw new BadRequestException($"Product {product.Name} ({variant.Sku}) has insufficient stock.");
            if (product.Weight is null or <= 0 || product.Width is null or <= 0
                || product.Length is null or <= 0 || product.Height is null or <= 0)
                throw new BadRequestException($"Product {product.Name} is missing weight or parcel dimensions.");

            for (var i = 0; i < item.Quantity; i++)
            {
                packableItems.Add(new PackableItem(
                    product.Width.Value,
                    product.Length.Value,
                    product.Height.Value,
                    product.Weight.Value));
            }

            lines.Add(new PricingLineInput(product.Id, variant.Id, variant.Sku, product.Name,
                item.Quantity, variant.SellPrice, variant.SellVatStatus, variant.ImageUrl, variant.OptionsJson,
                product.ZortCategoryId, product.CategoryName, product.ZortSubCategoryId, product.SubCategoryName));
        }

        var shippingChannel = EmptyToNull(payload.ShippingChannel);
        var packages = CheckoutPackagePacker.Pack(packableItems);
        var canPack = packages is not null;
        var hasShippingDetails = HasShippingDetails(payload);
        var shippingPackages = packages ?? [];
        if (canPack && hasShippingDetails && shippingChannel is null)
            shippingChannel = await ResolveDefaultShippingChannelAsync(payload, shippingPackages, cancellationToken);

        var shippingFinalized = shippingChannel is not null && hasShippingDetails && canPack;
        var status = canPack ? shippingFinalized ? "final" : "partial" : "manual_shipping_required";
        return new ResolvedCheckoutPricingPayload(lines, shippingFinalized, shippingFinalized, status, shippingChannel, shippingPackages);
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
        return parts.Length < 5
            ? null
            : new(BuildSpaceSeparated(parts[..^4]), parts[^4], parts[^3], parts[^2], parts[^1]);
    }

    private static object NormalizePayload(
        CheckoutPricingPayload payload,
        IReadOnlyCollection<PricingLineInput> lines,
        string? shippingChannel,
        IReadOnlyCollection<CheckoutShippingPackage> packages)
        => new
        {
            CustomerEmail = NormalizeNullable(payload.CustomerEmail),
            ShippingName = NormalizeNullable(payload.ShippingName),
            ShippingPhone = NormalizeNullable(payload.ShippingPhone),
            ShippingAddress = NormalizeNullable(payload.ShippingAddress),
            ShippingChannel = NormalizeNullable(shippingChannel),
            CouponCode = NormalizeNullable(payload.CouponCode),
            CouponCodes = (payload.CouponCodes ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct()
                .OrderBy(x => x)
                .ToArray(),
            Items = lines
                .OrderBy(x => x.ProductId)
                .ThenBy(x => x.VariantId)
                .Select(x => new
                {
                    x.ProductId,
                    x.VariantId,
                    x.Sku,
                    x.Quantity,
                    UnitPrice = Math.Round(x.UnitPrice, 2, MidpointRounding.AwayFromZero)
                })
                .ToArray(),
            Packages = NormalizePackages(packages)
        };

    private static object NormalizeCalculation(
        string payloadHash,
        bool shippingFinalized,
        PricingResult result,
        IReadOnlyCollection<CheckoutShippingPackage> packages)
        => new
        {
            PayloadHash = payloadHash,
            ShippingFinalized = shippingFinalized,
            Packages = NormalizePackages(packages),
            Lines = result.Lines
                .OrderBy(x => x.Input.ProductId)
                .ThenBy(x => x.Input.VariantId)
                .Select(x => new
                {
                    x.Input.ProductId,
                    x.Input.VariantId,
                    x.Input.Sku,
                    x.Input.Quantity,
                    BaseUnitPrice = Money(x.Input.UnitPrice),
                    UnitPrice = Money(x.UnitPrice),
                    DiscountAmount = Money(x.DiscountAmount),
                    Total = Money(x.Total)
                }),
            ItemSubtotal = Money(result.ItemSubtotal),
            ShippingAmount = Money(result.ShippingAmount),
            ShippingDiscountAmount = Money(result.ShippingDiscountAmount),
            CouponDiscountAmount = Money(result.CouponDiscountAmount),
            PromotionDiscountAmount = Money(result.PromotionDiscountAmount),
            OrderDiscountAmount = Money(result.OrderDiscountAmount),
            VatAmount = Money(result.VatAmount),
            GrandTotal = Money(result.GrandTotal),
            result.AppliedCouponId,
            AppliedCoupons = result.AppliedCoupons
                .OrderBy(x => x.Code)
                .Select(x => new
                {
                    x.CouponId,
                    x.Code,
                    x.Type,
                    DiscountAmount = Money(x.DiscountAmount)
                }),
            result.AppliedFlashSaleId,
            AppliedPromotions = result.AppliedPromotions
                .OrderBy(x => x.PromotionId)
                .Select(x => new
                {
                    x.PromotionId,
                    x.Name,
                    DiscountAmount = Money(x.DiscountAmount)
                }),
            Adjustments = result.Adjustments
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Code)
                .ThenBy(x => x.ProductId)
                .Select(x => new
                {
                    x.Type,
                    x.Code,
                    Amount = Money(x.Amount),
                    x.ProductId
                })
        };

    private static object NormalizePackages(IReadOnlyCollection<CheckoutShippingPackage> packages)
        => packages
            .OrderBy(x => x.BoxCode)
            .ThenBy(x => x.WidthCm)
            .ThenBy(x => x.LengthCm)
            .ThenBy(x => x.HeightCm)
            .Select(x => new
            {
                x.BoxCode,
                WidthCm = Money(x.WidthCm),
                LengthCm = Money(x.LengthCm),
                HeightCm = Money(x.HeightCm),
                WeightKg = Math.Round(x.WeightKg, 3, MidpointRounding.AwayFromZero),
                x.ItemCount
            })
            .ToArray();

    private static string Hash(object value)
    {
        var json = JsonSerializer.Serialize(value, HashJsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasShippingDetails(CheckoutPricingPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.ShippingName)
            || string.IsNullOrWhiteSpace(payload.ShippingPhone)
            || string.IsNullOrWhiteSpace(payload.ShippingAddress))
        {
            return false;
        }

        if (ParseAddress(payload.ShippingAddress) is null)
            throw new BadRequestException("ShippingAddress must end with district, state, province, and postcode.");

        return true;
    }

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal Money(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private async Task<string> ResolveDefaultShippingChannelAsync(
        CheckoutPricingPayload payload,
        IReadOnlyCollection<CheckoutShippingPackage> packages,
        CancellationToken cancellationToken)
    {
        var address = ParseAddress(payload.ShippingAddress!)
            ?? throw new BadRequestException("ShippingAddress must end with district, state, province, and postcode.");
        var optionTasks = packages
            .Select(package => shippingRates.GetShippingOptionsAsync(
                new ShippingRateQuoteRequest(
                    payload.ShippingName!.Trim(), payload.ShippingPhone!.Trim(),
                    EmptyToNull(payload.CustomerEmail),
                    address.Address, address.District, address.State, address.Province, address.Postcode,
                    $"Checkout pricing quote {package.BoxCode}",
                    (double)package.WeightKg, (double)package.WidthCm, (double)package.LengthCm,
                    (double)package.HeightCm, string.Empty),
                cancellationToken))
            .ToArray();
        var packageOptions = await Task.WhenAll(optionTasks);
        var packageCount = packageOptions.Length;
        var standard = packageOptions
            .SelectMany(x => x)
            .GroupBy(x => x.ShippingChannel, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() == packageCount)
            .Select(x => new
            {
                ShippingChannel = x.Key,
                TotalAmount = x.Sum(option => option.Amount)
            })
            .OrderBy(x => x.TotalAmount)
            .ThenBy(x => x.ShippingChannel, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return standard?.ShippingChannel
            ?? throw new BadRequestException("No shipping channel is available for this order.");
    }

    private static string BuildSpaceSeparated(IEnumerable<string> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(part.Trim());
        }

        return builder.ToString();
    }

    private sealed record ParsedAddress(string Address, string District, string State, string Province, string Postcode);
}

public sealed record CheckoutPricingPayload(
    string? CustomerEmail,
    string? ShippingName,
    string? ShippingPhone,
    string? ShippingAddress,
    string? ShippingChannel,
    string? CouponCode,
    IReadOnlyCollection<CheckoutPricingItem> Items,
    IReadOnlyCollection<string>? CouponCodes = null);

public sealed record CheckoutPricingItem(Guid ProductId, Guid? VariantId, int Quantity);

public sealed record CheckoutPricingQuoteDraft(
    PricingResult Pricing,
    string PayloadHash,
    string CalculationHash,
    bool ShippingFinalized,
    bool IsFinal,
    string CalculationStatus,
    string? ShippingChannel,
    IReadOnlyCollection<CheckoutShippingPackage> Packages);

public sealed record CheckoutPricingPayloadHash(
    string PayloadHash,
    bool ShippingFinalized,
    bool IsFinal,
    string CalculationStatus);

internal sealed record ResolvedCheckoutPricingPayload(
    IReadOnlyCollection<PricingLineInput> Lines,
    bool ShippingFinalized,
    bool IsFinal,
    string CalculationStatus,
    string? ShippingChannel,
    IReadOnlyCollection<CheckoutShippingPackage> Packages);
