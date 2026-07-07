namespace PonPon.Modules.Catalog.Domain.FlashSales;

public sealed class FlashSale
{
    private readonly List<FlashSaleProduct> _products = [];

    private FlashSale() { Name = string.Empty; Slots = []; }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string[] Slots { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<FlashSaleProduct> Products => _products.AsReadOnly();

    public static FlashSale Create(string name, DateOnly startDate, DateOnly endDate, string[] slots, IReadOnlyList<(Guid ProductId, decimal SalePrice, int? QuantityLimit)> products, DateTime now)
    {
        var flashSale = new FlashSale
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            Slots = slots,
            CreatedAt = now
        };
        flashSale.SetProducts(products);
        return flashSale;
    }

    public void Update(string name, DateOnly startDate, DateOnly endDate, string[] slots, IReadOnlyList<(Guid ProductId, decimal SalePrice, int? QuantityLimit)> products, DateTime now)
    {
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Slots = slots;
        UpdatedAt = now;
        _products.Clear();
        SetProducts(products);
    }

    private void SetProducts(IReadOnlyList<(Guid ProductId, decimal SalePrice, int? QuantityLimit)> products)
    {
        foreach (var (productId, salePrice, quantityLimit) in products)
            _products.Add(new FlashSaleProduct(Id, productId, salePrice, quantityLimit));
    }
}
