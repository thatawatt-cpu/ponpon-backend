using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ApproveManualRefund;

public static class ManualRefundAmountCalculator
{
    public static decimal Calculate(
        decimal paymentAmount,
        decimal alreadyRefundedAmount,
        decimal? requestedAmount)
    {
        var refundableAmount = paymentAmount - alreadyRefundedAmount;
        if (refundableAmount <= 0)
            throw new BadRequestException("This order has no refundable payment remaining.");

        var refundAmount = requestedAmount ?? refundableAmount;
        if (refundAmount <= 0)
            throw new BadRequestException("Refunded amount must be greater than zero.");

        if (refundAmount > refundableAmount)
        {
            throw new BadRequestException(
                $"Refunded amount cannot exceed the remaining refundable amount ({refundableAmount:0.00}).");
        }

        return refundAmount;
    }
}
