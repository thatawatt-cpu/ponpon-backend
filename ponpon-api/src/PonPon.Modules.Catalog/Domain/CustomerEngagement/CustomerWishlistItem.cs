using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.CustomerEngagement;

public sealed class CustomerWishlistItem : Entity
{
    private CustomerWishlistItem()
    {
    }

    private CustomerWishlistItem(Guid customerId, Guid productId, DateTime now)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        ProductId = productId;
        CreatedAtUtc = now;
    }

    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static CustomerWishlistItem Create(Guid customerId, Guid productId, DateTime now)
        => new(customerId, productId, now);
}
