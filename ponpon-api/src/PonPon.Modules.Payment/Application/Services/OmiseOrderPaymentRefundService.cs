using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application.Services;

public sealed class OmiseOrderPaymentRefundService : IOrderPaymentRefundService
{
    private readonly IOmiseClient _omise;
    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OmiseOrderPaymentRefundService> _logger;

    public OmiseOrderPaymentRefundService(
        IOmiseClient omise,
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<OmiseOrderPaymentRefundService> logger)
    {
        _omise = omise;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task RefundOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        await using var paymentLock = await _orders.AcquirePaymentLockAsync(orderId, cancellationToken);
        var order = await _orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        if (!string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(order.PaymentStatus, "1", StringComparison.OrdinalIgnoreCase))
        {
            await paymentLock.CompleteAsync(cancellationToken);
            return;
        }

        var chargeId = order.OmiseChargeId;
        if (string.IsNullOrWhiteSpace(chargeId))
        {
            var legacyCharges = await _omise.FindSuccessfulChargesByOrderNumberAsync(
                order.Number,
                cancellationToken);
            if (legacyCharges.Count == 0)
            {
                throw new BadRequestException(
                    "Paid order has no matching Omise charge. Refund must be handled manually.");
            }

            if (legacyCharges.Count > 1)
            {
                throw new BadRequestException(
                    "Paid order has multiple matching Omise charges. Refund must be handled manually.");
            }

            chargeId = legacyCharges.Single().ChargeId;
            order.RegisterOmiseCharge(chargeId, _clock.UtcNow);
        }

        var charge = await _omise.GetChargeAsync(chargeId, cancellationToken);
        if (!charge.Paid || !string.Equals(charge.Status, "successful", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Omise charge is not successful and cannot be refunded.");
        }

        if (!charge.Refundable)
        {
            throw new BadRequestException(
                "Payment could not be refunded automatically by Omise. Please refund this order manually before cancelling it.");
        }

        var remainingAmount = charge.Amount - charge.RefundedAmount;
        if (remainingAmount <= 0)
        {
            order.RecordOmiseRefund(
                order.OmiseRefundId,
                "closed",
                charge.Amount / 100m,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await paymentLock.CompleteAsync(cancellationToken);
            return;
        }

        var refund = await CreateRefundWithRecoveryAsync(
            charge.ChargeId,
            remainingAmount,
            order.Number,
            cancellationToken);
        var refundedCharge = await _omise.GetChargeAsync(charge.ChargeId, cancellationToken);
        if (refundedCharge.RefundedAmount < refundedCharge.Amount)
        {
            throw new BadRequestException(
                "Omise refund has not completed for the full charge amount.");
        }

        order.RecordOmiseRefund(
            refund?.RefundId,
            refund?.Status ?? "closed",
            refundedCharge.RefundedAmount / 100m,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await paymentLock.CompleteAsync(cancellationToken);

        _logger.LogInformation(
            "Omise order refund completed: OrderId={OrderId} OrderNumber={OrderNumber} ChargeId={ChargeId} RefundId={RefundId} Amount={Amount}",
            order.Id,
            order.Number,
            charge.ChargeId,
            refund?.RefundId,
            charge.Amount);
    }

    private async Task<OmiseRefundResult?> CreateRefundWithRecoveryAsync(
        string chargeId,
        int amount,
        string orderNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _omise.CreateRefundAsync(
                chargeId,
                amount,
                orderNumber,
                cancellationToken);
        }
        catch (BadRequestException)
        {
            var refreshedCharge = await _omise.GetChargeAsync(chargeId, cancellationToken);
            if (refreshedCharge.RefundedAmount < refreshedCharge.Amount)
            {
                throw;
            }

            _logger.LogInformation(
                "Omise refund was already completed for charge {ChargeId}; continuing cancellation idempotently.",
                chargeId);
            return null;
        }
    }
}
