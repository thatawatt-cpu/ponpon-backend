using PonPon.Shared.Domain;

namespace PonPon.Modules.Ordering.Domain.Orders;

public sealed class OrderItem : Entity
{
    private OrderItem()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    private OrderItem(Guid orderId, OrderItemSnapshot snapshot) : this()
    {
        OrderId = orderId;
        ZortProductId = snapshot.ZortProductId;
        Sku = snapshot.Sku;
        Name = snapshot.Name;
        Quantity = snapshot.Quantity;
        UnitText = snapshot.UnitText;
        PricePerUnit = snapshot.PricePerUnit;
        Discount = snapshot.Discount;
        DiscountAmount = snapshot.DiscountAmount;
        TotalPrice = snapshot.TotalPrice;
        ProductType = snapshot.ProductType;
        BundleId = snapshot.BundleId;
        BundleCode = snapshot.BundleCode;
        BundleName = snapshot.BundleName;
        RawZortJson = snapshot.RawZortJson;
    }

    public Guid OrderId { get; private set; }
    public long? ZortProductId { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public decimal Quantity { get; private set; }
    public string? UnitText { get; private set; }
    public decimal PricePerUnit { get; private set; }
    public string? Discount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice { get; private set; }
    public int ProductType { get; private set; }
    public long? BundleId { get; private set; }
    public string? BundleCode { get; private set; }
    public string? BundleName { get; private set; }
    public string RawZortJson { get; private set; } = "{}";

    internal static OrderItem FromSnapshot(Guid orderId, OrderItemSnapshot snapshot) => new(orderId, snapshot);
}

public sealed record OrderItemSnapshot(
    long? ZortProductId,
    string Sku,
    string Name,
    decimal Quantity,
    string? UnitText,
    decimal PricePerUnit,
    string? Discount,
    decimal DiscountAmount,
    decimal TotalPrice,
    int ProductType,
    long? BundleId,
    string? BundleCode,
    string? BundleName,
    string RawZortJson);
