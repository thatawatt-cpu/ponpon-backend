using PonPon.Shared.Domain;

namespace PonPon.Modules.Ordering.Domain.Orders;

public sealed class OrderPayment : Entity
{
    private OrderPayment()
    {
        Name = string.Empty;
    }

    private OrderPayment(Guid orderId, OrderPaymentSnapshot snapshot) : this()
    {
        OrderId = orderId;
        ZortPaymentId = snapshot.ZortPaymentId;
        Name = snapshot.Name;
        Amount = snapshot.Amount;
        PaymentDateTime = snapshot.PaymentDateTime;
        RawZortJson = snapshot.RawZortJson;
    }

    public Guid OrderId { get; private set; }
    public long? ZortPaymentId { get; private set; }
    public string Name { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime? PaymentDateTime { get; private set; }
    public string RawZortJson { get; private set; } = "{}";

    internal static OrderPayment FromSnapshot(Guid orderId, OrderPaymentSnapshot snapshot) => new(orderId, snapshot);
}

public sealed record OrderPaymentSnapshot(
    long? ZortPaymentId,
    string Name,
    decimal Amount,
    DateTime? PaymentDateTime,
    string RawZortJson);
