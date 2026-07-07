namespace PonPon.Modules.Promotion.Domain;

public sealed class CouponUsage
{
    private CouponUsage() { }

    public Guid Id { get; private set; }
    public Guid CouponId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public bool IsReleased { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }

    public static CouponUsage Reserve(
        Guid couponId,
        Guid orderId,
        Guid customerId,
        decimal discountAmount,
        DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            CouponId = couponId,
            OrderId = orderId,
            CustomerId = customerId,
            DiscountAmount = discountAmount,
            CreatedAtUtc = now
        };

    public void Release(DateTime now)
    {
        IsReleased = true;
        ReleasedAtUtc = now;
    }
}
