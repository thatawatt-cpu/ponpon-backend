namespace PonPon.Modules.Catalog.Domain.FlashSales;

public sealed class FlashSaleProduct
{
    private FlashSaleProduct() { }

    public FlashSaleProduct(Guid flashSaleId, Guid productId, decimal salePrice, int? quantityLimit)
    {
        FlashSaleId = flashSaleId;
        ProductId = productId;
        SalePrice = salePrice;
        QuantityLimit = quantityLimit;
    }

    public Guid FlashSaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal SalePrice { get; private set; }
    public int? QuantityLimit { get; private set; }
    public int ReservedQuantity { get; private set; }
}
