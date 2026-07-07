using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncPendingOrderToZort;
using PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Modules.Promotion.Application;

namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed class AddOrderHandler
{
    private static readonly Guid DevCustomerId = new("77e02c7d-9578-47e8-bd07-e4e336e1e1c9");

    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IProductRepository _products;
    private readonly IBackgroundTaskQueue _backgroundQueue;
    private readonly ICurrentUser _currentUser;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IShippingRateQuoteService _shippingRates;
    private readonly IDateTimeProvider _clock;
    private readonly IWebHostEnvironment _env;
    private readonly PricingPipeline _pricingPipeline;
    private readonly ICouponService _coupons;
    private readonly IPromotionService _promotions;
    private readonly IFlashSaleRepository _flashSales;

    public AddOrderHandler(
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IProductRepository products,
        IBackgroundTaskQueue backgroundQueue,
        ICurrentUser currentUser,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        IShippingRateQuoteService shippingRates,
        PricingPipeline pricingPipeline,
        ICouponService coupons,
        IPromotionService promotions,
        IFlashSaleRepository flashSales,
        IDateTimeProvider clock,
        IWebHostEnvironment env)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _products = products;
        _backgroundQueue = backgroundQueue;
        _currentUser = currentUser;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _shippingRates = shippingRates;
        _pricingPipeline = pricingPipeline;
        _coupons = coupons;
        _promotions = promotions;
        _flashSales = flashSales;
        _clock = clock;
        _env = env;
    }

    public async Task<AddOrderResponse> HandleAsync(
        AddOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        Validate(command);
        var zortOrderId = CreateTemporaryZortOrderId(command.ClientRequestId);
        var existingOrder = await _orders.GetByZortOrderIdAsync(zortOrderId, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.CustomerId != customerId)
                throw new BadRequestException("ClientRequestId has already been used.");

            return ToResponse(existingOrder);
        }

        var requestedItems = command.Items
            .GroupBy(x => new { x.ProductId, x.VariantId })
            .Select(x => new AddOrderItemCommand(x.Key.ProductId, x.Key.VariantId, x.Sum(item => item.Quantity)))
            .ToArray();

        var zortItems = new List<ZortAddOrderItemRequest>(requestedItems.Length);
        var catalogDataBySku = new Dictionary<string, (Guid ProductId, Guid VariantId, string? ImageUrl, string? OptionsJson)>();
        var stockToDecrement = new Dictionary<Guid, int>();
        var pricingLines = new List<PricingLineInput>(requestedItems.Length);
        decimal totalWeightGrams = 0;
        decimal maxWidth = 0;
        decimal maxLength = 0;
        decimal maxHeight = 0;

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

            if (product.Weight is null or <= 0
                || product.Width is null or <= 0
                || product.Length is null or <= 0
                || product.Height is null or <= 0)
            {
                throw new BadRequestException(
                    $"Product {product.Name} is missing weight or parcel dimensions.");
            }

            totalWeightGrams += product.Weight.Value * requestedItem.Quantity;
            maxWidth = Math.Max(maxWidth, product.Width.Value);
            maxLength = Math.Max(maxLength, product.Length.Value);
            maxHeight = Math.Max(maxHeight, product.Height.Value);

            zortItems.Add(new ZortAddOrderItemRequest(
                variant.Sku,
                product.Name,
                requestedItem.Quantity,
                variant.SellPrice,
                0,
                variant.SellPrice * requestedItem.Quantity));
            pricingLines.Add(new PricingLineInput(
                product.Id,
                variant.Id,
                variant.Sku,
                product.Name,
                requestedItem.Quantity,
                variant.SellPrice,
                variant.SellVatStatus,
                variant.ImageUrl,
                variant.OptionsJson,
                product.ZortCategoryId,
                product.CategoryName,
                product.ZortSubCategoryId,
                product.SubCategoryName));
        }

        var shippingAddress = ParseShippingAddress(command.ShippingAddress)
            ?? throw new BadRequestException(
                "ShippingAddress must end with district, state, province, and postcode.");
        var shippingChannel = command.ShippingChannel?.Trim();
        if (string.IsNullOrWhiteSpace(shippingChannel))
            throw new BadRequestException("ShippingChannel is required.");

        var shippingAmount = await _shippingRates.GetShippingAmountAsync(
            new ShippingRateQuoteRequest(
                command.ShippingName.Trim(),
                command.ShippingPhone.Trim(),
                EmptyToNull(command.CustomerEmail),
                shippingAddress.Address,
                shippingAddress.District,
                shippingAddress.State,
                shippingAddress.Province,
                shippingAddress.Postcode,
                $"Order {command.ClientRequestId:N}",
                (double)(totalWeightGrams / 1000m),
                (double)maxWidth,
                (double)maxLength,
                (double)maxHeight,
                shippingChannel),
            cancellationToken);

        var pricing = await _pricingPipeline.ExecuteAsync(
            new PricingContext(
                pricingLines,
                shippingAmount,
                command.CouponCode,
                _clock.UtcNow,
                customerId,
                SyncOrdersFromZortHandler.LineLiffSalesChannel,
                command.PaymentMethod,
                shippingChannel,
                command.CouponCodes),
            cancellationToken);
        zortItems = pricing.Lines
            .Select(x => new ZortAddOrderItemRequest(
                x.Input.Sku,
                x.Input.Name,
                x.Input.Quantity,
                x.UnitPrice,
                x.DiscountAmount,
                x.Total))
            .ToList();
        var number = $"LIFF-{command.ClientRequestId:N}";
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
                throw new BadRequestException("Coupon quota is no longer available.");
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

        _backgroundQueue.Enqueue(async (sp, ct) =>
        {
            var handler = sp.GetRequiredService<SyncPendingOrderToZortHandler>();
            await handler.HandleAsync(order.Id, ct);
        });

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

    private Guid GetCustomerId()
    {
        if (_currentUser.IsAuthenticated
            && _currentUser.UserType == "Customer"
            && _currentUser.CustomerId is Guid customerId)
        {
            return customerId;
        }

        if (_env.IsDevelopment())
        {
            return DevCustomerId;
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

    private static void Validate(AddOrderCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
        {
            throw new BadRequestException("ClientRequestId is required.");
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
}
