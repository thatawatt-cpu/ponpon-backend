namespace PonPon.Modules.Promotion.Domain;

public sealed class PromotionUsage
{
    private PromotionUsage() { }

    public Guid Id { get; private set; }
    public Guid PromotionId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public bool IsReleased { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }

    public static PromotionUsage Reserve(
        Guid promotionId,
        Guid orderId,
        Guid customerId,
        decimal discountAmount,
        DateTime now)
    {
        return new PromotionUsage
        {
            Id = Guid.NewGuid(),
            PromotionId = promotionId,
            OrderId = orderId,
            CustomerId = customerId,
            DiscountAmount = discountAmount,
            CreatedAtUtc = now
        };
    }

    public void Release(DateTime now)
    {
        if (IsReleased)
            return;

        IsReleased = true;
        ReleasedAtUtc = now;
    }
}
