using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Payment.Application;

public static class OrderPaymentAmountCalculator
{
    public static decimal Calculate(Order order)
    {
        return order.Amount < 0 ? 0 : order.Amount;
    }
}
