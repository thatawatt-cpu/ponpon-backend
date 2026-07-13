using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.CustomerEngagement;

public sealed class CustomerRecentlyViewedProduct : Entity
{
    private CustomerRecentlyViewedProduct()
    {
    }

    private CustomerRecentlyViewedProduct(Guid customerId, Guid productId, DateTime viewedAtUtc)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        ProductId = productId;
        ViewedAtUtc = viewedAtUtc;
    }

    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime ViewedAtUtc { get; private set; }

    public static CustomerRecentlyViewedProduct Create(Guid customerId, Guid productId, DateTime viewedAtUtc)
        => new(customerId, productId, viewedAtUtc);

    public void Touch(DateTime viewedAtUtc)
    {
        ViewedAtUtc = viewedAtUtc;
    }
}
