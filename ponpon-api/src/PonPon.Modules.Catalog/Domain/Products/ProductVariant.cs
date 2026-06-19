using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.Products;

public sealed class ProductVariant : Entity
{
    private ProductVariant()
    {
        BaseSku = string.Empty;
        Sku = string.Empty;
    }

    private ProductVariant(Guid productId, string baseSku, string? variantCode, ProductSnapshot snapshot, DateTime now) : this()
    {
        ProductId = productId;
        CreatedAt = now;
        ApplyZortSnapshot(baseSku, variantCode, snapshot, now);
    }

    public Guid ProductId { get; private set; }
    public long? ZortProductId { get; private set; }
    public long? ZortVariationId { get; private set; }
    public string BaseSku { get; private set; }
    public string Sku { get; private set; }
    public string? VariantCode { get; private set; }
    public string? Barcode { get; private set; }
    public decimal SellPrice { get; private set; }
    public int SellVatStatus { get; private set; }
    public decimal? PurchasePrice { get; private set; }
    public int PurchaseVatStatus { get; private set; }
    public int Stock { get; private set; }
    public int AvailableStock { get; private set; }
    public string? UnitText { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActiveFromZort { get; private set; }
    public ProductStatus Status { get; private set; }
    public string? RawZortJson { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsVisibleToCustomer => IsActiveFromZort && Status == ProductStatus.Active && AvailableStock > 0;

    public static ProductVariant CreateFromZort(Guid productId, string baseSku, string? variantCode, ProductSnapshot snapshot, DateTime now)
    {
        return new ProductVariant(productId, baseSku, variantCode, snapshot, now);
    }

    public void ApplyZortSnapshot(string baseSku, string? variantCode, ProductSnapshot snapshot, DateTime now)
    {
        ZortProductId = snapshot.ZortProductId;
        ZortVariationId = snapshot.ZortVariationId;
        BaseSku = string.IsNullOrWhiteSpace(baseSku) ? snapshot.Sku : baseSku;
        Sku = string.IsNullOrWhiteSpace(snapshot.Sku) ? $"ZORT-{snapshot.ZortProductId}" : snapshot.Sku;
        VariantCode = variantCode;
        Barcode = snapshot.Barcode;
        SellPrice = snapshot.SellPrice;
        SellVatStatus = snapshot.SellVatStatus;
        PurchasePrice = snapshot.PurchasePrice;
        PurchaseVatStatus = snapshot.PurchaseVatStatus;
        Stock = snapshot.Stock;
        AvailableStock = snapshot.AvailableStock;
        UnitText = snapshot.UnitText;
        ImageUrl = snapshot.ImageUrl;
        IsActiveFromZort = snapshot.IsActiveFromZort;
        Status = snapshot.IsActiveFromZort ? ProductStatus.Active : ProductStatus.Inactive;
        RawZortJson = snapshot.RawZortJson;
        LastSyncedAt = now;
        UpdatedAt = now;
    }
}
