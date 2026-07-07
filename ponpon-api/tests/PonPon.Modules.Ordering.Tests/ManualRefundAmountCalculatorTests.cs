using PonPon.Modules.Ordering.Application.Features.Orders.ApproveManualRefund;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Tests;

public sealed class ManualRefundAmountCalculatorTests
{
    public void DefaultsToRemainingRefundableAmount()
    {
        var amount = ManualRefundAmountCalculator.Calculate(500m, 125m, null);
        AssertEqual(375m, amount);
    }

    public void RejectsRefundAboveRemainingAmount()
    {
        AssertThrows<BadRequestException>(
            () => ManualRefundAmountCalculator.Calculate(500m, 125m, 376m));
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
