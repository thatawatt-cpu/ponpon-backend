namespace PonPon.Modules.Catalog.Domain.FlashSales;

public sealed class FlashSaleReservation
{
    private FlashSaleReservation() { }

    public FlashSaleReservation(Guid orderId, Guid flashSaleId, Guid productId, int quantity, DateTime now)
    {
        OrderId = orderId;
        FlashSaleId = flashSaleId;
        ProductId = productId;
        Quantity = quantity;
        CreatedAtUtc = now;
    }

    public Guid OrderId { get; private set; }
    public Guid FlashSaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public bool IsReleased { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }

    public void Release(DateTime now)
    {
        if (IsReleased) return;
        IsReleased = true;
        ReleasedAtUtc = now;
    }
}
