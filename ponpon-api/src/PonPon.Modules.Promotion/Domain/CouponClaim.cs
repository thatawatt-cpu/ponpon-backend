namespace PonPon.Modules.Promotion.Domain;

public sealed class CouponClaim
{
    private CouponClaim() { }

    public Guid Id { get; private set; }
    public Guid CouponId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime ClaimedAtUtc { get; private set; }

    public static CouponClaim Create(Guid couponId, Guid customerId, DateTime nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            CouponId = couponId,
            CustomerId = customerId,
            ClaimedAtUtc = nowUtc
        };
}
