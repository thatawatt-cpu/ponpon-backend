using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Ordering.Application.Features.Orders.CheckoutPricing;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Modules.Promotion.Application;
using System.Globalization;
using System.Text.Json;

namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed class AddOrderHandler
{
    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IDateTimeProvider _clock;
    private readonly CheckoutPricingQuoteService _quoteService;
    private readonly ICheckoutQuoteRepository _quotes;
    private readonly ICouponService _coupons;
    private readonly IPromotionService _promotions;
    private readonly IFlashSaleRepository _flashSales;

    public AddOrderHandler(
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IProductRepository products,
        ICurrentUser currentUser,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        CheckoutPricingQuoteService quoteService,
        ICheckoutQuoteRepository quotes,
        ICouponService coupons,
        IPromotionService promotions,
        IFlashSaleRepository flashSales,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _products = products;
        _currentUser = currentUser;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _quoteService = quoteService;
        _quotes = quotes;
        _coupons = coupons;
        _promotions = promotions;
        _flashSales = flashSales;
        _clock = clock;
    }

    public async Task<AddOrderResponse> HandleAsync(
        AddOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        Validate(command);
        await using var clientRequestLock = await _orders.AcquireClientRequestLockAsync(
            command.ClientRequestId,
            cancellationToken);
        var zortOrderId = CreateTemporaryZortOrderId(command.ClientRequestId);
        var existingOrder = await _orders.GetByZortOrderIdAsync(zortOrderId, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.CustomerId != customerId)
                throw new BadRequestException("ClientRequestId has already been used.");

            await clientRequestLock.CompleteAsync(cancellationToken);
            return ToResponse(existingOrder);
        }

        var requestedItems = command.Items
            .GroupBy(x => new { x.ProductId, x.VariantId })
            .Select(x => new AddOrderItemCommand(x.Key.ProductId, x.Key.VariantId, x.Sum(item => item.Quantity)))
            .ToArray();

        var zortItems = new List<ZortAddOrderItemRequest>(requestedItems.Length);
        var catalogDataBySku = new Dictionary<string, (Guid ProductId, Guid VariantId, string? ImageUrl, string? OptionsJson)>();
        var stockToDecrement = new Dictionary<Guid, int>();

        foreach (var requestedItem in requestedItems)
        {
            var product = await _products.GetByIdWithVariantsAsync(requestedItem.ProductId, cancellationToken)
                ?? throw new BadRequestException($"Product {requestedItem.ProductId} was not found.");

            if (!product.IsVisibleToCustomer)
            {
                throw new BadRequestException($"Product {product.Name} is not available.");
            }

            var variant = ResolveVariant(product, requestedItem.VariantId);
            if (requestedItem.Quantity > variant.AvailableStock)
            {
                throw new BadRequestException($"Product {product.Name} ({variant.Sku}) has insufficient stock.");
            }

            catalogDataBySku[variant.Sku] = (product.Id, variant.Id, variant.ImageUrl, variant.OptionsJson);
            stockToDecrement[variant.Id] = requestedItem.Quantity;

            zortItems.Add(new ZortAddOrderItemRequest(
                variant.Sku,
                product.Name,
                requestedItem.Quantity,
                variant.SellPrice,
                0,
                variant.SellPrice * requestedItem.Quantity));
        }

        var shippingChannel = command.ShippingChannel?.Trim();
        if (string.IsNullOrWhiteSpace(shippingChannel))
            throw new BadRequestException("ShippingChannel is required.");

        var payloadHash = await _quoteService.CalculatePayloadHashAsync(
            new CheckoutPricingPayload(
                command.CustomerEmail,
                command.ShippingName,
                command.ShippingPhone,
                command.ShippingAddress,
                shippingChannel,
                command.CouponCode,
                requestedItems.Select(x => new CheckoutPricingItem(x.ProductId, x.VariantId, x.Quantity)).ToArray(),
                command.CouponCodes),
            cancellationToken);
        var quote = await ValidateQuoteAsync(command.QuoteId, customerId, payloadHash, cancellationToken);
        var pricing = CreatePricingResultFromQuote(quote.PricingSnapshotJson, zortItems);
        zortItems = pricing.Lines
            .Select(x => new ZortAddOrderItemRequest(
                x.Input.Sku,
                x.Input.Name,
                x.Input.Quantity,
                x.UnitPrice,
                x.DiscountAmount,
                x.Total))
            .ToList();
        var number = CreateLineLiffOrderNumber(_clock.UtcNow, zortItems[0].Sku);
        var snapshot = CreatePendingSnapshot(
            zortOrderId,
            number,
            command,
            pricing,
            zortItems,
            catalogDataBySku,
            _currentUser.LineUserId,
            _clock.UtcNow);

        var order = Order.CreateFromZort(snapshot, _clock.UtcNow);
        order.SetPricingSnapshot(pricing.SnapshotJson, _clock.UtcNow);
        order.SetCheckoutPaymentMethod(command.PaymentMethod, _clock.UtcNow);
        if (pricing.AppliedFlashSaleId.HasValue)
        {
            var flashProductIds = pricing.Adjustments
                .Where(x => x.Type == "flash_sale" && x.ProductId.HasValue)
                .Select(x => x.ProductId!.Value)
                .ToHashSet();
            var flashQuantities = pricing.Lines
                .Where(x => flashProductIds.Contains(x.Input.ProductId))
                .GroupBy(x => x.Input.ProductId)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.Input.Quantity));
            if (!await _flashSales.TryReserveQuotaAsync(
                    order.Id, pricing.AppliedFlashSaleId.Value, flashQuantities, _clock.UtcNow, cancellationToken))
                throw new BadRequestException("Flash sale quota is no longer available.");
        }
        foreach (var coupon in pricing.AppliedCoupons)
        {
            if (!await _coupons.TryReserveAsync(
                    coupon.CouponId,
                    order.Id,
                    customerId,
                    cancellationToken,
                    coupon.DiscountAmount))
            {
                await _coupons.ReleaseByOrderAsync(order.Id, CancellationToken.None);
                await _flashSales.ReleaseQuotaByOrderAsync(order.Id, _clock.UtcNow, CancellationToken.None);
                throw new BadRequestException(
                    "Coupon quota is no longer available.",
                    "coupon_quota_no_longer_available",
                    new { couponId = coupon.CouponId, couponCode = coupon.Code });
            }
        }
        foreach (var promotion in pricing.AppliedPromotions)
        {
            if (!await _promotions.TryReserveAsync(
                    promotion.PromotionId,
                    order.Id,
                    customerId,
                    cancellationToken,
                    promotion.DiscountAmount))
            {
                await _coupons.ReleaseByOrderAsync(order.Id, CancellationToken.None);
                await _promotions.ReleaseByOrderAsync(order.Id, CancellationToken.None);
                await _flashSales.ReleaseQuotaByOrderAsync(order.Id, _clock.UtcNow, CancellationToken.None);
                throw new BadRequestException("Promotion quota is no longer available.");
            }
        }

        if (!await _products.TryReserveVariantsStockAsync(stockToDecrement, cancellationToken))
        {
            await _coupons.ReleaseByOrderAsync(order.Id, CancellationToken.None);
            await _promotions.ReleaseByOrderAsync(order.Id, CancellationToken.None);
            await _flashSales.ReleaseQuotaByOrderAsync(order.Id, _clock.UtcNow, CancellationToken.None);
            throw new BadRequestException("One or more products have insufficient stock.");
        }

        order.MarkStockReserved(_clock.UtcNow);
        await _orders.AddAsync(order, cancellationToken);
        quote.MarkUsed(command.ClientRequestId, _clock.UtcNow);

        order.AssignCustomer(customerId, _clock.UtcNow);
        order.SetPaymentExpiry(_clock.UtcNow.AddMinutes(30), _clock.UtcNow);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _products.ReleaseVariantsStockAsync(stockToDecrement, CancellationToken.None);
            await _coupons.ReleaseByOrderAsync(order.Id, CancellationToken.None);
            await _promotions.ReleaseByOrderAsync(order.Id, CancellationToken.None);
            await _flashSales.ReleaseQuotaByOrderAsync(order.Id, _clock.UtcNow, CancellationToken.None);
            throw;
        }
        await clientRequestLock.CompleteAsync(cancellationToken);

        await _shopRealtimeNotifications.NotifyAsync(
            CreateShopNotification(
                order,
                "order_created",
                "สร้างคำสั่งซื้อแล้ว",
                "กรุณาชำระเงินภายในเวลาที่กำหนด",
                order.Status),
            cancellationToken);

        return ToResponse(order);
    }

    private static AddOrderResponse ToResponse(Order order)
    {
        return new AddOrderResponse(
            order.Id,
            order.ZortOrderId,
            order.Number,
            order.Status,
            order.PaymentStatus,
            order.Amount,
            order.ShippingAmount,
            order.DiscountAmount,
            order.PaymentExpiresAt);
    }

    private async Task<PonPon.Modules.Ordering.Domain.Quotes.CheckoutQuote> ValidateQuoteAsync(
        Guid quoteId,
        Guid customerId,
        CheckoutPricingPayloadHash payloadHash,
        CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetByIdAsync(quoteId, cancellationToken)
            ?? throw QuoteError(
                "Quote was not found.",
                "quote_not_found",
                new { quoteId });

        if (quote.CustomerId != customerId)
            throw QuoteError(
                "Quote does not belong to this customer.",
                "quote_customer_mismatch",
                new { quoteId });

        if (quote.ExpiresAtUtc <= _clock.UtcNow)
            throw QuoteError(
                "Quote has expired.",
                "quote_expired",
                new { quoteId, quote.ExpiresAtUtc });

        if (!quote.IsFinal || !quote.ShippingFinalized)
            throw QuoteError(
                "Quote is not final.",
                "quote_not_final",
                new
                {
                    quoteId,
                    quote.IsFinal,
                    quote.ShippingFinalized,
                    quote.CalculationStatus
                });

        if (!payloadHash.IsFinal || !payloadHash.ShippingFinalized)
            throw QuoteError(
                "Checkout payload does not have finalized shipping.",
                "quote_shipping_not_finalized",
                new
                {
                    quoteId,
                    payloadHash.IsFinal,
                    payloadHash.ShippingFinalized,
                    payloadHash.CalculationStatus
                });

        if (!string.Equals(quote.PayloadHash, payloadHash.PayloadHash, StringComparison.Ordinal))
            throw QuoteError(
                "Quote no longer matches the checkout payload.",
                "quote_mismatch",
                new
                {
                    quoteId,
                    expectedCalculationStatus = quote.CalculationStatus,
                    actualCalculationStatus = payloadHash.CalculationStatus
                });

        return quote;
    }

    private static BadRequestException QuoteError(string message, string code, object details)
        => new(message, code, details);

    private static PricingResult CreatePricingResultFromQuote(
        string pricingSnapshotJson,
        IReadOnlyCollection<ZortAddOrderItemRequest> currentItems)
    {
        var snapshot = JsonSerializer.Deserialize<StoredPricingSnapshot>(pricingSnapshotJson)
            ?? throw new BadRequestException("Quote pricing snapshot is invalid.", "quote_pricing_snapshot_invalid", new { });

        var currentItemsBySku = currentItems.ToDictionary(x => x.Sku, StringComparer.OrdinalIgnoreCase);
        var lines = snapshot.Lines.Select(line =>
        {
            if (!currentItemsBySku.TryGetValue(line.Sku, out var currentItem))
                throw new BadRequestException(
                    "Quote no longer matches the checkout payload.",
                    "quote_mismatch",
                    new { sku = line.Sku });

            return new PricedLine(
                new PricingLineInput(
                    line.ProductId,
                    line.VariantId,
                    line.Sku,
                    currentItem.Name,
                    line.Quantity,
                    line.BaseUnitPrice,
                    line.SellVatStatus,
                    null,
                    null,
                    line.ZortCategoryId,
                    line.CategoryName,
                    line.ZortSubCategoryId,
                    line.SubCategoryName),
                line.UnitPrice,
                line.DiscountAmount,
                line.Total);
        }).ToArray();

        return new PricingResult(
            lines,
            snapshot.ItemSubtotal,
            snapshot.ShippingAmount,
            snapshot.ShippingDiscountAmount,
            snapshot.OrderDiscountAmount,
            snapshot.OrderDiscountAmount - snapshot.PromotionDiscountAmount,
            snapshot.PromotionDiscountAmount,
            snapshot.AppliedCouponId,
            snapshot.AppliedCoupons,
            snapshot.AppliedFlashSaleId,
            snapshot.AppliedPromotions,
            snapshot.VatAmount,
            snapshot.GrandTotal,
            snapshot.Adjustments,
            pricingSnapshotJson);
    }

    private Guid GetCustomerId()
    {
        if (_currentUser.IsAuthenticated
            && _currentUser.UserType == "Customer"
            && _currentUser.CustomerId is Guid customerId)
        {
            return customerId;
        }

        throw new UnauthorizedException("Customer authentication is required.");
    }

    private static long CreateTemporaryZortOrderId(Guid clientRequestId)
    {
        var bytes = clientRequestId.ToByteArray();
        var rawValue = BitConverter.ToInt64(bytes, 0);
        var value = rawValue == long.MinValue ? long.MaxValue : Math.Abs(rawValue);
        return -Math.Max(1, value);
    }

    private static string CreateLineLiffOrderNumber(DateTime utcNow, string sku)
    {
        var bangkokNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            ResolveBangkokTimeZone());

        return string.Concat(
            "LLPP-",
            bangkokNow.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture),
            sku.Trim());
    }

    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static void Validate(AddOrderCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
        {
            throw new BadRequestException("ClientRequestId is required.");
        }

        if (command.QuoteId == Guid.Empty)
        {
            throw new BadRequestException("QuoteId is required.", "quote_required", new { field = "quoteId" });
        }

        if (string.IsNullOrWhiteSpace(command.CustomerName)
            || string.IsNullOrWhiteSpace(command.CustomerPhone)
            || string.IsNullOrWhiteSpace(command.CustomerAddress)
            || string.IsNullOrWhiteSpace(command.ShippingName)
            || string.IsNullOrWhiteSpace(command.ShippingPhone)
            || string.IsNullOrWhiteSpace(command.ShippingAddress))
        {
            throw new BadRequestException("Customer and shipping information are required.");
        }

        if (command.Items.Count == 0 || command.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
        {
            throw new BadRequestException("At least one valid order item is required.");
        }
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OrderSnapshot CreatePendingSnapshot(
        long zortOrderId,
        string number,
        AddOrderCommand command,
        PricingResult pricing,
        IReadOnlyCollection<ZortAddOrderItemRequest> zortItems,
        IReadOnlyDictionary<string, (Guid ProductId, Guid VariantId, string? ImageUrl, string? OptionsJson)> catalogDataBySku,
        string? lineUserId,
        DateTime now)
    {
        var items = zortItems.Select(item =>
        {
            catalogDataBySku.TryGetValue(item.Sku, out var catalog);
            return new OrderItemSnapshot(
                null,
                item.Sku,
                item.Name,
                item.Quantity,
                null,
                item.PricePerUnit,
                null,
                item.Discount,
                item.TotalPrice,
                0,
                null,
                null,
                null,
                "{}",
                catalog.ProductId == Guid.Empty ? null : catalog.ProductId,
                catalog.VariantId == Guid.Empty ? null : catalog.VariantId,
                catalog.ImageUrl,
                catalog.OptionsJson);
        }).ToArray();

        return new OrderSnapshot(
            zortOrderId,
            number,
            null,
            null,
            command.CustomerName.Trim(),
            null,
            EmptyToNull(command.CustomerEmail),
            command.CustomerPhone.Trim(),
            command.CustomerAddress.Trim(),
            ZortOrderStatus.Pending.ToString(),
            ZortPaymentStatus.Pending.ToString(),
            pricing.GrandTotal,
            pricing.VatAmount,
            pricing.ShippingAmount,
            0,
            pricing.OrderDiscountAmount,
            EmptyToNull(command.ShippingChannel),
            command.ShippingName.Trim(),
            command.ShippingAddress.Trim(),
            command.ShippingPhone.Trim(),
            null,
            now,
            null,
            command.ClientRequestId.ToString("N"),
            EmptyToNull(command.Description),
            SyncOrdersFromZortHandler.LineLiffSalesChannel,
            lineUserId,
            command.CustomerName.Trim(),
            null,
            false,
            "THB",
            null,
            now,
            now,
            items,
            [],
            "{}");
    }

    private static ParsedShippingAddress? ParseShippingAddress(string shippingAddress)
    {
        var parts = shippingAddress
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 5)
            return null;

        var address = string.Join(' ', parts[..^4]);
        return string.IsNullOrWhiteSpace(address)
            ? null
            : new ParsedShippingAddress(address, parts[^4], parts[^3], parts[^2], parts[^1]);
    }

    private sealed record ParsedShippingAddress(
        string Address,
        string District,
        string State,
        string Province,
        string Postcode);

    private static ProductVariant ResolveVariant(Product product, Guid? variantId)
    {
        if (variantId is Guid id)
        {
            return product.FindVariant(id)
                ?? throw new BadRequestException($"Variant {id} was not found for product {product.Id}.");
        }

        var visibleVariants = product.Variants.Where(x => x.IsVisibleToCustomer).ToArray();
        if (visibleVariants.Length == 1)
        {
            return visibleVariants[0];
        }

        if (visibleVariants.Length == 0)
        {
            throw new BadRequestException($"Product {product.Name} has no available variants.");
        }

        throw new BadRequestException($"Product {product.Name} requires a variantId.");
    }

    private static ShopRealtimeNotification CreateShopNotification(
        Order order,
        string type,
        string title,
        string message,
        string? status)
        => new(
            order.CustomerId,
            order.IntegrationCustomerId,
            type,
            order.Id,
            order.Number,
            title,
            message,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            status);

    private sealed record StoredPricingSnapshot(
        DateTime NowUtc,
        string? SalesChannel,
        string? PaymentMethod,
        string? ShippingChannel,
        IReadOnlyCollection<StoredPricingLine> Lines,
        decimal ItemSubtotal,
        decimal ShippingAmount,
        decimal ShippingDiscountAmount,
        decimal OrderDiscountAmount,
        decimal PromotionDiscountAmount,
        Guid? AppliedCouponId,
        IReadOnlyCollection<AppliedCoupon> AppliedCoupons,
        Guid? AppliedFlashSaleId,
        IReadOnlyCollection<AppliedPromotion> AppliedPromotions,
        decimal VatAmount,
        decimal GrandTotal,
        IReadOnlyCollection<PriceAdjustment> Adjustments);

    private sealed record StoredPricingLine(
        Guid ProductId,
        Guid VariantId,
        string Sku,
        int Quantity,
        decimal BaseUnitPrice,
        decimal UnitPrice,
        decimal DiscountAmount,
        decimal Total,
        int SellVatStatus,
        long? ZortCategoryId,
        string? CategoryName,
        long? ZortSubCategoryId,
        string? SubCategoryName);
}
