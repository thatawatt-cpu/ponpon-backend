using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.Products;

public sealed class ProductImage : Entity
{
    private ProductImage()
    {
        Url = string.Empty;
    }

    public ProductImage(Guid productId, string url, int sortOrder, bool isPrimary, DateTime createdAt)
    {
        ProductId = productId;
        Url = url;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        CreatedAt = createdAt;
    }

    public Guid ProductId { get; private set; }
    public string Url { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
