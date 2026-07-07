using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application;

internal static class OrderPaymentSecurity
{
    public static int ToSatang(decimal amount)
    {
        return checked((int)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }

    public static async Task<OmiseChargeResult?> GetReusableChargeAsync(
        Order order,
        IOmiseClient omise,
        int expectedAmountSatang,
        string expectedSourceType,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (IsPaid(order.PaymentStatus))
            throw new BadRequestException("Order has already been paid.");

        if (IsVoided(order.Status) || IsVoided(order.PaymentStatus))
            throw new BadRequestException("A payment cannot be created for a cancelled order.");

        if (order.PaymentExpiresAt is DateTime expiresAt && expiresAt <= now)
            throw new BadRequestException("The payment period for this order has expired.");

        var existingCharges = string.IsNullOrWhiteSpace(order.OmiseChargeId)
            ? await omise.FindChargesByOrderNumberAsync(order.Number, cancellationToken)
            : [await omise.GetChargeAsync(order.OmiseChargeId, cancellationToken)];

        var activeCharge = existingCharges.FirstOrDefault(x => IsActive(x, now));
        if (activeCharge is null)
            return null;

        ValidateCharge(order, activeCharge, expectedAmountSatang);
        if (!IsExpectedSource(activeCharge.SourceType, expectedSourceType))
            throw new BadRequestException("An active payment charge already exists with a different payment method.");
        return activeCharge;
    }

    public static void ValidateCharge(
        Order order,
        OmiseChargeResult charge,
        int expectedAmountSatang)
    {
        if (string.IsNullOrWhiteSpace(charge.ChargeId)
            || charge.Amount != expectedAmountSatang
            || !string.Equals(charge.Currency, "THB", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(charge.Description?.Trim(), order.Number, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Omise charge details do not match this order.");
        }
    }

    public static void ValidatePaymentMethod(
        OmiseChargeResult charge,
        string expectedSourceType)
    {
        if (!IsExpectedSource(charge.SourceType, expectedSourceType))
            throw new BadRequestException("Omise charge payment method does not match the requested payment method.");
    }

    private static bool IsPaid(string status)
        => string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "1", StringComparison.OrdinalIgnoreCase);

    private static bool IsVoided(string status)
        => string.Equals(status, "Voided", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "2", StringComparison.OrdinalIgnoreCase);

    private static bool IsActive(OmiseChargeResult charge, DateTime now)
    {
        var isExpired = charge.ExpiresAt is DateTime expiresAt && expiresAt <= now;
        return !charge.Voided
            && !isExpired
            && !string.Equals(charge.Status, "failed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExpectedSource(string? actualSourceType, string expectedSourceType)
    {
        if (string.Equals(expectedSourceType, "card", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(actualSourceType)
                || string.Equals(actualSourceType, "card", StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(actualSourceType, expectedSourceType, StringComparison.OrdinalIgnoreCase);
    }
}
