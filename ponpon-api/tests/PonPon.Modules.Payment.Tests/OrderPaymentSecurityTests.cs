using PonPon.Modules.Payment.Application;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Tests;

public sealed class OrderPaymentSecurityTests
{
    public void ConvertsBahtToSatangUsingExplicitRounding()
    {
        AssertEqual(2001, OrderPaymentSecurity.ToSatang(20.005m));
    }

    public void RejectsReusingChargeFromDifferentPaymentMethod()
    {
        var charge = new OmiseChargeResult(
            "chrg_test",
            "pending",
            false,
            null,
            null,
            "https://example.test/qr",
            null,
            null,
            DateTime.UtcNow.AddMinutes(10),
            2000,
            "THB",
            false,
            0,
            false,
            "promptpay",
            "ORDER-1");

        AssertThrows<BadRequestException>(
            () => OrderPaymentSecurity.ValidatePaymentMethod(charge, "card"));
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
}
