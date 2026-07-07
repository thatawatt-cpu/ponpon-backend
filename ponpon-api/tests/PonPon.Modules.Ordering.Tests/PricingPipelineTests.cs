using Microsoft.Extensions.Options;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Shared.Application.Exceptions;
using PonPon.Modules.Promotion.Application;
using PonPon.Modules.Promotion.Domain;
using PromotionEntity = PonPon.Modules.Promotion.Domain.Promotion;

namespace PonPon.Modules.Ordering.Tests;

public sealed class PricingPipelineTests
{
    public void AppliesFlashSaleCouponVatAndFinalTotal()
    {
        var productId = Guid.NewGuid();
        var flashSale = FlashSale.Create(
            "Lunch sale",
            new DateOnly(2026, 7, 3),
            new DateOnly(2026, 7, 3),
            [],
            [(productId, 80m, null)],
            DateTime.UtcNow);
        var options = Options.Create(new PricingOptions
        {
            VatEnabled = true,
            VatRate = 0.07m,
            PricesIncludeVat = true,
            ShippingVatApplicable = true,
        });
        var coupon = Coupon.Create(
            new CouponInput("SAVE10", "percentage", 10, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline(
        [
            new FlashSalePricingStep(new FakeFlashSaleRepository(flashSale)),
            new CouponPricingStep(new FakeCouponService(coupon)),
            new VatPricingStep(options),
            new FinalizePricingStep(options)
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(
                productId,
                Guid.NewGuid(),
                "SKU-1",
                "Tea",
                2,
                100m,
                1,
                null,
                null)
        ],
        20m,
        "SAVE10",
        new DateTime(2026, 7, 3, 5, 0, 0, DateTimeKind.Utc));

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(80m, result.Lines.Single().UnitPrice);
        AssertEqual(16m, result.OrderDiscountAmount);
        AssertEqual(164m, result.GrandTotal);
        AssertEqual(10.73m, result.VatAmount);
    }

    public void RejectsCouponThatCannotCombineWithFlashSale()
    {
        var productId = Guid.NewGuid();
        var flashSale = FlashSale.Create(
            "Sale",
            new DateOnly(2026, 7, 3),
            new DateOnly(2026, 7, 3),
            [],
            [(productId, 80m, null)],
            DateTime.UtcNow);
        var options = Options.Create(new PricingOptions());
        var coupon = Coupon.Create(
            new CouponInput("NO-STACK", "fixed", 10, 0, null, null, null, false, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline(
        [
            new FlashSalePricingStep(new FakeFlashSaleRepository(flashSale)),
            new CouponPricingStep(new FakeCouponService(coupon))
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(productId, Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        0,
        "NO-STACK",
        new DateTime(2026, 7, 3, 5, 0, 0, DateTimeKind.Utc));

        AssertThrows<BadRequestException>(
            () => pipeline.ExecuteAsync(context).GetAwaiter().GetResult());
    }

    public void AppliesCouponOnlyToEligibleProductScope()
    {
        var eligibleProductId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var coupon = Coupon.Create(
            new CouponInput(
                "TEA10",
                "percentage",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                [new CouponScopeInput("product", ProductId: eligibleProductId)]),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([new CouponPricingStep(new FakeCouponService(coupon))]);
        var context = new PricingContext(
        [
            new PricingLineInput(eligibleProductId, Guid.NewGuid(), "TEA", "Tea", 1, 100m, 0, null, null),
            new PricingLineInput(otherProductId, Guid.NewGuid(), "CAKE", "Cake", 1, 200m, 0, null, null)
        ],
        0,
        "TEA10",
        DateTime.UtcNow);

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(10m, result.OrderDiscountAmount);
    }

    public void RejectsCouponWhenNoLineMatchesScope()
    {
        var coupon = Coupon.Create(
            new CouponInput(
                "ONLY-TEA",
                "fixed",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                [new CouponScopeInput("sku", Sku: "TEA")]),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([new CouponPricingStep(new FakeCouponService(coupon))]);
        var context = new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "CAKE", "Cake", 1, 200m, 0, null, null)
        ],
        0,
        "ONLY-TEA",
        DateTime.UtcNow);

        AssertThrows<BadRequestException>(
            () => pipeline.ExecuteAsync(context).GetAwaiter().GetResult());
    }

    public void CalculatesVatDiscountFromEligibleScopeOnly()
    {
        var eligibleProductId = Guid.NewGuid();
        var coupon = Coupon.Create(
            new CouponInput(
                "NONTAX10",
                "fixed",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                [new CouponScopeInput("product", ProductId: eligibleProductId)]),
            DateTime.UtcNow);
        var options = Options.Create(new PricingOptions
        {
            VatEnabled = true,
            VatRate = 0.07m,
            PricesIncludeVat = true,
            ShippingVatApplicable = false
        });
        var pipeline = new PricingPipeline(
        [
            new CouponPricingStep(new FakeCouponService(coupon)),
            new VatPricingStep(options),
            new FinalizePricingStep(options)
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(eligibleProductId, Guid.NewGuid(), "NONTAX", "No VAT", 1, 100m, 0, null, null),
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "TAX", "VAT", 1, 100m, 1, null, null)
        ],
        0,
        "NONTAX10",
        DateTime.UtcNow);

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(10m, result.OrderDiscountAmount);
        AssertEqual(6.54m, result.VatAmount);
    }

    public void AppliesFreeShippingCoupon()
    {
        var coupon = Coupon.Create(
            new CouponInput(
                "SHIPFREE", "free_shipping", 0, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var options = Options.Create(new PricingOptions());
        var pipeline = new PricingPipeline(
        [
            new CouponPricingStep(new FakeCouponService(coupon)),
            new FinalizePricingStep(options)
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        50m,
        "SHIPFREE",
        DateTime.UtcNow);

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(50m, result.CouponDiscountAmount);
        AssertEqual(50m, result.ShippingDiscountAmount);
        AssertEqual(100m, result.GrandTotal);
    }

    public void CapsFreeShippingCouponDiscount()
    {
        var coupon = Coupon.Create(
            new CouponInput(
                "SHIP50", "free_shipping", 0, 0, 50m, null, null, true, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline(
        [
            new CouponPricingStep(new FakeCouponService(coupon)),
            new FinalizePricingStep(Options.Create(new PricingOptions()))
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        80m,
        "SHIP50",
        DateTime.UtcNow);

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(50m, result.ShippingDiscountAmount);
        AssertEqual(130m, result.GrandTotal);
    }

    public void ExcludesFreeShippingCouponFromVatBase()
    {
        var coupon = Coupon.Create(
            new CouponInput(
                "SHIPFREE", "free_shipping", 0, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var options = Options.Create(new PricingOptions
        {
            VatEnabled = true,
            VatRate = 0.07m,
            PricesIncludeVat = true,
            ShippingVatApplicable = true
        });
        var pipeline = new PricingPipeline(
        [
            new CouponPricingStep(new FakeCouponService(coupon)),
            new VatPricingStep(options),
            new FinalizePricingStep(options)
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 107m, 1, null, null)
        ],
        21.40m,
        "SHIPFREE",
        DateTime.UtcNow);

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(7m, result.VatAmount);
        AssertEqual(107m, result.GrandTotal);
    }

    public void AppliesNewCustomerCouponWhenCustomerHasNoCompletedOrders()
    {
        var customerId = Guid.NewGuid();
        var coupon = Coupon.Create(
            new CouponInput(
                "NEW10",
                "fixed",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                CustomerScopes: [new CouponCustomerScopeInput("new_customer")]),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new CouponPricingStep(new FakeCouponService(coupon), new FakeOrderRepository(0))
        ]);

        var result = pipeline.ExecuteAsync(CreateCustomerContext("NEW10", customerId)).GetAwaiter().GetResult();

        AssertEqual(10m, result.OrderDiscountAmount);
    }

    public void RejectsNewCustomerCouponWhenCustomerHasCompletedOrders()
    {
        var customerId = Guid.NewGuid();
        var coupon = Coupon.Create(
            new CouponInput(
                "NEW10",
                "fixed",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                CustomerScopes: [new CouponCustomerScopeInput("new_customer")]),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new CouponPricingStep(new FakeCouponService(coupon), new FakeOrderRepository(1))
        ]);

        AssertThrows<BadRequestException>(
            () => pipeline.ExecuteAsync(CreateCustomerContext("NEW10", customerId)).GetAwaiter().GetResult());
    }

    public void AppliesSpecificCustomerCouponOnlyToThatCustomer()
    {
        var customerId = Guid.NewGuid();
        var coupon = Coupon.Create(
            new CouponInput(
                "VIP10",
                "fixed",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                CustomerScopes: [new CouponCustomerScopeInput("customer", customerId)]),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new CouponPricingStep(new FakeCouponService(coupon), new FakeOrderRepository(99))
        ]);

        var result = pipeline.ExecuteAsync(CreateCustomerContext("VIP10", customerId)).GetAwaiter().GetResult();

        AssertEqual(10m, result.OrderDiscountAmount);
        AssertThrows<BadRequestException>(
            () => pipeline.ExecuteAsync(CreateCustomerContext("VIP10", Guid.NewGuid())).GetAwaiter().GetResult());
    }

    public void AppliesCouponWhenCheckoutConditionsMatch()
    {
        var coupon = CreateConditionalCoupon();
        var pipeline = new PricingPipeline([new CouponPricingStep(new FakeCouponService(coupon))]);
        var context = CreateConditionContext("promptpay", "flash");

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(10m, result.OrderDiscountAmount);
    }

    public void RejectsCouponWhenAnyCheckoutConditionDoesNotMatch()
    {
        var coupon = CreateConditionalCoupon();
        var pipeline = new PricingPipeline([new CouponPricingStep(new FakeCouponService(coupon))]);

        AssertThrows<BadRequestException>(() =>
            pipeline.ExecuteAsync(CreateConditionContext("card", "flash")).GetAwaiter().GetResult());
        AssertThrows<BadRequestException>(() =>
            pipeline.ExecuteAsync(CreateConditionContext("promptpay", "standard")).GetAwaiter().GetResult());
    }

    public void AppliesWeeklyAutoPromotionWithoutCoupon()
    {
        var productId = Guid.NewGuid();
        var promotion = PromotionEntity.Create(
            new PromotionInput(
                "Tuesday tea",
                null,
                "auto_discount",
                "percentage",
                10,
                0,
                null,
                null,
                null,
                "Asia/Bangkok",
                10,
                true,
                true,
                true,
                null,
                null,
                true,
                ScheduleRules: [new PromotionScheduleRuleInput("day_of_week", DayOfWeek: 2)],
                Scopes: [new PromotionScopeInput("product", ProductId: productId)]),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new AutoPromotionPricingStep(new FakePromotionService(promotion)),
            new FinalizePricingStep(Options.Create(new PricingOptions()))
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(productId, Guid.NewGuid(), "TEA", "Tea", 2, 100m, 0, null, null)
        ],
        0,
        null,
        new DateTime(2026, 7, 7, 5, 0, 0, DateTimeKind.Utc));

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(20m, result.PromotionDiscountAmount);
        AssertEqual(20m, result.OrderDiscountAmount);
        AssertEqual(180m, result.GrandTotal);
    }

    public void RejectsCouponWhenPromotionCannotStackWithCoupon()
    {
        var promotion = PromotionEntity.Create(
            new PromotionInput(
                "No coupon stack",
                null,
                "auto_discount",
                "fixed",
                20,
                0,
                null,
                null,
                null,
                "Asia/Bangkok",
                10,
                false,
                true,
                true,
                null,
                null,
                true),
            DateTime.UtcNow);
        var coupon = Coupon.Create(
            new CouponInput("SAVE10", "fixed", 10, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new AutoPromotionPricingStep(new FakePromotionService(promotion)),
            new CouponPricingStep(new FakeCouponService(coupon))
        ]);

        AssertThrows<BadRequestException>(() =>
            pipeline.ExecuteAsync(CreateCustomerContext("SAVE10", Guid.NewGuid())).GetAwaiter().GetResult());
    }

    public void RejectsCouponWhenCouponCannotStackWithPromotion()
    {
        var promotion = PromotionEntity.Create(
            new PromotionInput(
                "Stackable promotion",
                null,
                "auto_discount",
                "fixed",
                20,
                0,
                null,
                null,
                null,
                "Asia/Bangkok",
                10,
                true,
                true,
                true,
                null,
                null,
                true),
            DateTime.UtcNow);
        var coupon = Coupon.Create(
            new CouponInput(
                "COUPON-EXCLUSIVE",
                "fixed",
                10,
                0,
                null,
                null,
                null,
                true,
                null,
                null,
                true,
                CanStackWithPromotions: false),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new AutoPromotionPricingStep(new FakePromotionService(promotion)),
            new CouponPricingStep(new FakeCouponService(coupon))
        ]);

        AssertThrows<BadRequestException>(() =>
            pipeline.ExecuteAsync(CreateCustomerContext("COUPON-EXCLUSIVE", Guid.NewGuid())).GetAwaiter().GetResult());
    }

    public void StopsAtFirstNonStackingPromotionByPriority()
    {
        var high = PromotionEntity.Create(
            new PromotionInput("High", null, "auto_discount", "fixed", 30, 0, null,
                null, null, "Asia/Bangkok", 100, true, false, true, null, null, true),
            DateTime.UtcNow);
        var low = PromotionEntity.Create(
            new PromotionInput("Low", null, "auto_discount", "fixed", 20, 0, null,
                null, null, "Asia/Bangkok", 1, true, true, true, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new AutoPromotionPricingStep(new FakePromotionService(high, low)),
            new FinalizePricingStep(Options.Create(new PricingOptions()))
        ]);

        var result = pipeline.ExecuteAsync(CreateCustomerContext(null, Guid.NewGuid())).GetAwaiter().GetResult();

        AssertEqual(30m, result.PromotionDiscountAmount);
        AssertEqual(1, result.AppliedPromotions.Count);
    }

    public void AppliesOneDiscountCouponAndOneFreeShippingCoupon()
    {
        var discountCoupon = Coupon.Create(
            new CouponInput("SAVE10", "fixed", 10, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var shippingCoupon = Coupon.Create(
            new CouponInput("FREESHIP", "free_shipping", 0, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new CouponPricingStep(new FakeCouponService(discountCoupon, shippingCoupon)),
            new FinalizePricingStep(Options.Create(new PricingOptions()))
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        50m,
        null,
        DateTime.UtcNow,
        Guid.NewGuid(),
        couponCodes: ["SAVE10", "FREESHIP"]);

        var result = pipeline.ExecuteAsync(context).GetAwaiter().GetResult();

        AssertEqual(60m, result.CouponDiscountAmount);
        AssertEqual(50m, result.ShippingDiscountAmount);
        AssertEqual(90m, result.GrandTotal);
        AssertEqual(2, result.AppliedCoupons.Count);
    }

    public void RejectsTwoDiscountCoupons()
    {
        var firstCoupon = Coupon.Create(
            new CouponInput("SAVE10", "fixed", 10, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var secondCoupon = Coupon.Create(
            new CouponInput("SAVE20", "fixed", 20, 0, null, null, null, true, null, null, true),
            DateTime.UtcNow);
        var pipeline = new PricingPipeline([
            new CouponPricingStep(new FakeCouponService(firstCoupon, secondCoupon))
        ]);
        var context = new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        50m,
        null,
        DateTime.UtcNow,
        Guid.NewGuid(),
        couponCodes: ["SAVE10", "SAVE20"]);

        AssertThrows<BadRequestException>(() =>
            pipeline.ExecuteAsync(context).GetAwaiter().GetResult());
    }

    private static Coupon CreateConditionalCoupon() => Coupon.Create(
        new CouponInput(
            "CHANNEL10",
            "fixed",
            10,
            0,
            null,
            null,
            null,
            true,
            null,
            null,
            true,
            Conditions:
            [
                new CouponConditionInput("sales_channel", "LineLiff"),
                new CouponConditionInput("payment_method", "promptpay"),
                new CouponConditionInput("shipping_channel", "flash")
            ]),
        DateTime.UtcNow);

    private static PricingContext CreateConditionContext(string paymentMethod, string shippingChannel)
    {
        return new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        0,
        "CHANNEL10",
        DateTime.UtcNow,
        Guid.NewGuid(),
        "LineLiff",
        paymentMethod,
        shippingChannel);
    }

    private static PricingContext CreateCustomerContext(string? couponCode, Guid customerId)
    {
        return new PricingContext(
        [
            new PricingLineInput(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Tea", 1, 100m, 0, null, null)
        ],
        0,
        couponCode,
        DateTime.UtcNow,
        customerId);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private sealed class FakeFlashSaleRepository : IFlashSaleRepository
    {
        private readonly FlashSale _flashSale;

        public FakeFlashSaleRepository(FlashSale flashSale) => _flashSale = flashSale;
        public Task<IReadOnlyCollection<FlashSale>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<FlashSale>>([_flashSale]);
        public Task<FlashSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<FlashSale?>(_flashSale.Id == id ? _flashSale : null);
        public Task<FlashSale?> GetActiveAsync(DateOnly today, CancellationToken cancellationToken = default)
            => Task.FromResult<FlashSale?>(_flashSale);
        public Task AddAsync(FlashSale flashSale, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task DeleteProductsAsync(Guid flashSaleId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<bool> TryReserveQuotaAsync(Guid orderId, Guid flashSaleId, IReadOnlyDictionary<Guid, int> productQuantities, DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task ReleaseQuotaByOrderAsync(Guid orderId, DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeCouponService : ICouponService
    {
        private readonly IReadOnlyCollection<Coupon> _coupons;
        public FakeCouponService(params Coupon[] coupons) => _coupons = coupons;
        public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
            => Task.FromResult<Coupon?>(_coupons.FirstOrDefault(x =>
                string.Equals(code, x.Code, StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyCollection<Coupon>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_coupons);
        public Task<IReadOnlyCollection<Coupon>> GetByCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Coupon>>(
                _coupons.Where(x => x.CampaignId == campaignId).ToArray());
        public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Coupon?>(_coupons.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyCollection<CouponUsage>> GetUsagesAsync(Guid couponId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CouponUsage>>([]);
        public Task<IReadOnlyCollection<CouponAuditLog>> GetAuditLogsAsync(Guid couponId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CouponAuditLog>>([]);
        public Task<int> GetActiveCustomerUsageCountAsync(Guid couponId, Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
        public Task<Guid> CreateAsync(CouponInput input, CancellationToken cancellationToken = default)
            => Task.FromResult(_coupons.FirstOrDefault()?.Id ?? Guid.NewGuid());
        public Task<CouponBulkGenerateResult> BulkGenerateAsync(CouponBulkGenerateInput input, CancellationToken cancellationToken = default)
            => Task.FromResult(new CouponBulkGenerateResult(Guid.NewGuid(), input.CampaignId, 0, []));
        public Task UpdateAsync(Guid id, CouponInput input, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<bool> TryReserveAsync(
            Guid couponId,
            Guid orderId,
            Guid customerId,
            CancellationToken cancellationToken = default,
            decimal discountAmount = 0)
            => Task.FromResult(true);
        public Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakePromotionService : IPromotionService
    {
        private readonly IReadOnlyCollection<PromotionEntity> _promotions;

        public FakePromotionService(params PromotionEntity[] promotions) => _promotions = promotions;

        public Task<IReadOnlyCollection<PromotionEntity>> GetAllAsync(Guid? campaignId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<PromotionEntity>>(_promotions);

        public Task<IReadOnlyCollection<PromotionEntity>> GetActiveAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<PromotionEntity>>(
                _promotions
                    .Where(x => x.IsActive
                        && (!x.StartsAtUtc.HasValue || nowUtc >= x.StartsAtUtc.Value)
                        && (!x.EndsAtUtc.HasValue || nowUtc <= x.EndsAtUtc.Value))
                    .OrderByDescending(x => x.Priority)
                    .ToArray());

        public Task<PromotionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<PromotionEntity?>(_promotions.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyCollection<PromotionUsage>> GetUsagesAsync(Guid promotionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<PromotionUsage>>([]);

        public Task<Guid> CreateAsync(PromotionInput input, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid());

        public Task UpdateAsync(Guid id, PromotionInput input, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> GetActiveCustomerUsageCountAsync(Guid promotionId, Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<bool> TryReserveAsync(Guid promotionId, Guid orderId, Guid customerId, CancellationToken cancellationToken = default, decimal discountAmount = 0)
            => Task.FromResult(true);

        public Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly int _completedOrderCount;

        public FakeOrderRepository(int completedOrderCount) => _completedOrderCount = completedOrderCount;

        public Task<int> CountCustomerCompletedOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_completedOrderCount);

        public Task<IReadOnlyCollection<AdminOrderListItem>> GetAsync(
            string? keyword,
            string? status,
            string? paymentStatus,
            string? returnRequestStatus,
            string? refundRequestStatus,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Order?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CustomerOrderListProjection> GetCustomerOrdersAsync(
            Guid customerId,
            IReadOnlyCollection<string>? statuses,
            IReadOnlyCollection<string>? paymentStatuses,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Order?> GetCustomerOrderByIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Order?> GetByZortOrderIdAsync(long zortOrderId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ReloadAsync(Order order, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<Order>> GetPendingZortSyncAsync(int limit, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> UpdatePaymentStatusAsync(
            Guid orderId,
            string expectedOmiseChargeId,
            string orderStatus,
            string paymentStatus,
            decimal paymentAmount,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<Order>> GetExpiredUnpaidAsync(DateTime now, string salesChannel, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IOrderPaymentLock> AcquirePaymentLockAsync(Guid orderId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
