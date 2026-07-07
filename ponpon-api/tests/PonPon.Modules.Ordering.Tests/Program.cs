using PonPon.Modules.Ordering.Tests;

var tests = new (string Name, Action Run)[]
{
    (nameof(ZortOrderMapperTests.MapsOrderItemsAndPayments),
        new ZortOrderMapperTests().MapsOrderItemsAndPayments),
    (nameof(ManualRefundAmountCalculatorTests.DefaultsToRemainingRefundableAmount),
        new ManualRefundAmountCalculatorTests().DefaultsToRemainingRefundableAmount),
    (nameof(ManualRefundAmountCalculatorTests.RejectsRefundAboveRemainingAmount),
        new ManualRefundAmountCalculatorTests().RejectsRefundAboveRemainingAmount),
    (nameof(PricingPipelineTests.AppliesFlashSaleCouponVatAndFinalTotal),
        new PricingPipelineTests().AppliesFlashSaleCouponVatAndFinalTotal),
    (nameof(PricingPipelineTests.RejectsCouponThatCannotCombineWithFlashSale),
        new PricingPipelineTests().RejectsCouponThatCannotCombineWithFlashSale),
    (nameof(PricingPipelineTests.AppliesCouponOnlyToEligibleProductScope),
        new PricingPipelineTests().AppliesCouponOnlyToEligibleProductScope),
    (nameof(PricingPipelineTests.RejectsCouponWhenNoLineMatchesScope),
        new PricingPipelineTests().RejectsCouponWhenNoLineMatchesScope),
    (nameof(PricingPipelineTests.CalculatesVatDiscountFromEligibleScopeOnly),
        new PricingPipelineTests().CalculatesVatDiscountFromEligibleScopeOnly),
    (nameof(PricingPipelineTests.AppliesFreeShippingCoupon),
        new PricingPipelineTests().AppliesFreeShippingCoupon),
    (nameof(PricingPipelineTests.CapsFreeShippingCouponDiscount),
        new PricingPipelineTests().CapsFreeShippingCouponDiscount),
    (nameof(PricingPipelineTests.ExcludesFreeShippingCouponFromVatBase),
        new PricingPipelineTests().ExcludesFreeShippingCouponFromVatBase),
    (nameof(PricingPipelineTests.AppliesNewCustomerCouponWhenCustomerHasNoCompletedOrders),
        new PricingPipelineTests().AppliesNewCustomerCouponWhenCustomerHasNoCompletedOrders),
    (nameof(PricingPipelineTests.RejectsNewCustomerCouponWhenCustomerHasCompletedOrders),
        new PricingPipelineTests().RejectsNewCustomerCouponWhenCustomerHasCompletedOrders),
    (nameof(PricingPipelineTests.AppliesSpecificCustomerCouponOnlyToThatCustomer),
        new PricingPipelineTests().AppliesSpecificCustomerCouponOnlyToThatCustomer),
    (nameof(PricingPipelineTests.AppliesCouponWhenCheckoutConditionsMatch),
        new PricingPipelineTests().AppliesCouponWhenCheckoutConditionsMatch),
    (nameof(PricingPipelineTests.RejectsCouponWhenAnyCheckoutConditionDoesNotMatch),
        new PricingPipelineTests().RejectsCouponWhenAnyCheckoutConditionDoesNotMatch),
    (nameof(PricingPipelineTests.AppliesWeeklyAutoPromotionWithoutCoupon),
        new PricingPipelineTests().AppliesWeeklyAutoPromotionWithoutCoupon),
    (nameof(PricingPipelineTests.RejectsCouponWhenPromotionCannotStackWithCoupon),
        new PricingPipelineTests().RejectsCouponWhenPromotionCannotStackWithCoupon),
    (nameof(PricingPipelineTests.RejectsCouponWhenCouponCannotStackWithPromotion),
        new PricingPipelineTests().RejectsCouponWhenCouponCannotStackWithPromotion),
    (nameof(PricingPipelineTests.StopsAtFirstNonStackingPromotionByPriority),
        new PricingPipelineTests().StopsAtFirstNonStackingPromotionByPriority),
    (nameof(PricingPipelineTests.AppliesOneDiscountCouponAndOneFreeShippingCoupon),
        new PricingPipelineTests().AppliesOneDiscountCouponAndOneFreeShippingCoupon),
    (nameof(PricingPipelineTests.RejectsTwoDiscountCoupons),
        new PricingPipelineTests().RejectsTwoDiscountCoupons)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}
