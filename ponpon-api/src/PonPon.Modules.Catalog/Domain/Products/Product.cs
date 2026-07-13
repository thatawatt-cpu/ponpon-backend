using PonPon.Shared.Domain;

namespace PonPon.Modules.Catalog.Domain.Products;

public sealed class Product : AggregateRoot, IAuditableEntity
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductVariant> _variants = [];

    private Product()
    {
        Name = string.Empty;
    }

    private Product(DateTime now)
    {
        Name = string.Empty;
        Source = ProductSource.Zort;
        Status = ProductStatus.Active;
        IsVisibleOnLiff = false;
        CreatedAt = now;
    }

    public long? ZortProductId { get; private set; }
    public string? BaseSku { get; private set; }
    public int ProductType { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? Barcode { get; private set; }
    public decimal SellPrice { get; private set; }
    public int SellVatStatus { get; private set; }
    public decimal? PurchasePrice { get; private set; }
    public int PurchaseVatStatus { get; private set; }
    public int Stock { get; private set; }
    public int AvailableStock { get; private set; }
    public string? UnitText { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal? Height { get; private set; }
    public decimal? Length { get; private set; }
    public decimal? Width { get; private set; }
    public long? ZortCategoryId { get; private set; }
    public string? CategoryName { get; private set; }
    public long? ZortSubCategoryId { get; private set; }
    public string? SubCategoryName { get; private set; }
    public long? ZortVariationId { get; private set; }
    public bool IsActiveFromZort { get; private set; }
    public bool IsVisibleOnLiff { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsBestSeller { get; private set; }
    public bool IsOnHomepage { get; private set; }
    public string? Slug { get; private set; }
    public decimal? OriginalPrice { get; private set; }
    public string? PromotionBadge { get; private set; }
    public string? Highlights { get; private set; }
    public string? RichDescription { get; private set; }
    public ProductSource Source { get; private set; }
    public ProductStatus Status { get; private set; }
    public string? RawZortJson { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public DateTime? MissingFromZortAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public DateTime CreatedAtUtc { get => CreatedAt; set => CreatedAt = value; }
    public DateTime? UpdatedAtUtc { get => UpdatedAt; set => UpdatedAt = value; }

    public bool IsVisibleToCustomer => IsActiveFromZort && IsVisibleOnLiff && Status == ProductStatus.Active && AvailableStock > 0;

    public static Product CreateFromZort(ProductSnapshot snapshot, DateTime now)
    {
        var product = new Product(now);
        product.ApplyZortSnapshot(snapshot, now);
        return product;
    }

    public void ApplyZortSnapshot(ProductSnapshot snapshot, DateTime now)
    {
        var sku = ParseSku(snapshot.Sku);
        ZortProductId = snapshot.ZortProductId;
        BaseSku = sku.BaseSku;
        ProductType = snapshot.ProductType;
        Name = string.IsNullOrWhiteSpace(snapshot.Name) ? "Untitled product" : snapshot.Name;
        Description = snapshot.Description;
        Barcode = snapshot.Barcode;
        SellPrice = snapshot.SellPrice;
        SellVatStatus = snapshot.SellVatStatus;
        PurchasePrice = snapshot.PurchasePrice;
        PurchaseVatStatus = snapshot.PurchaseVatStatus;
        Stock = snapshot.Stock;
        AvailableStock = snapshot.AvailableStock;
        UnitText = snapshot.UnitText;
        ImageUrl = snapshot.ImageUrl;
        Weight = snapshot.Weight;
        Height = snapshot.Height;
        Length = snapshot.Length;
        Width = snapshot.Width;
        ZortCategoryId = snapshot.ZortCategoryId;
        CategoryName = snapshot.CategoryName;
        ZortSubCategoryId = snapshot.ZortSubCategoryId;
        SubCategoryName = snapshot.SubCategoryName;
        ZortVariationId = snapshot.ZortVariationId;
        IsActiveFromZort = snapshot.IsActiveFromZort;
        Source = ProductSource.Zort;
        Status = snapshot.IsActiveFromZort ? ProductStatus.Active : ProductStatus.Inactive;
        RawZortJson = snapshot.RawZortJson;
        LastSyncedAt = now;
        MissingFromZortAt = null;
        UpdatedAt = now;
    }

    public void UpsertVariant(ProductSnapshot snapshot, DateTime now)
    {
        var sku = ParseSku(snapshot.Sku);
        var fullSku = string.IsNullOrWhiteSpace(snapshot.Sku) ? $"ZORT-{snapshot.ZortProductId}" : snapshot.Sku;
        var variant = _variants.FirstOrDefault(x => x.Sku == fullSku || (snapshot.ZortProductId.HasValue && x.ZortProductId == snapshot.ZortProductId));
        if (variant is null)
        {
            _variants.Add(ProductVariant.CreateFromZort(Id, sku.BaseSku, sku.VariantCode, snapshot, now));
            return;
        }

        variant.ApplyZortSnapshot(sku.BaseSku, sku.VariantCode, snapshot, now);
    }

    public ProductVariant? FindVariant(Guid variantId)
    {
        return _variants.FirstOrDefault(x => x.Id == variantId);
    }

    public void SetVisibility(bool isVisible, DateTime now)
    {
        IsVisibleOnLiff = isVisible;
        UpdatedAt = now;
    }

    public void AddImages(IReadOnlyList<(string Url, int SortOrder, bool IsPrimary)> images, DateTime now)
    {
        foreach (var (url, sortOrder, isPrimary) in images)
        {
            _images.Add(new ProductImage(Id, url, sortOrder, isPrimary, now));
        }
        UpdatedAt = now;
    }

    public void UpdatePonPonSettings(
        string? slug,
        decimal? originalPrice,
        string? promotionBadge,
        string? highlights,
        string? richDescription,
        bool isFeatured,
        bool isBestSeller,
        bool isOnHomepage,
        DateTime now)
    {
        Slug = slug;
        OriginalPrice = originalPrice;
        PromotionBadge = promotionBadge;
        Highlights = highlights;
        RichDescription = richDescription;
        IsFeatured = isFeatured;
        IsBestSeller = isBestSeller;
        IsOnHomepage = isOnHomepage;
        UpdatedAt = now;
    }

    public void MarkMissingFromZort(DateTime now)
    {
        IsActiveFromZort = false;
        Status = ProductStatus.MissingFromSource;
        MissingFromZortAt = now;
        UpdatedAt = now;
    }

    public static ParsedSku ParseSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return new ParsedSku(string.Empty, null);
        }

        var parts = sku.Trim().Split('-', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return new ParsedSku(sku.Trim(), null);
        }

        return new ParsedSku(parts[0], parts[1]);
    }
}

public sealed record ParsedSku(string BaseSku, string? VariantCode);

public sealed record ProductSnapshot(
    long? ZortProductId,
    int ProductType,
    string Name,
    string? Description,
    string Sku,
    string? Barcode,
    decimal SellPrice,
    int SellVatStatus,
    decimal? PurchasePrice,
    int PurchaseVatStatus,
    int Stock,
    int AvailableStock,
    string? UnitText,
    string? ImageUrl,
    decimal? Weight,
    decimal? Height,
    decimal? Length,
    decimal? Width,
    long? ZortCategoryId,
    string? CategoryName,
    long? ZortSubCategoryId,
    string? SubCategoryName,
    long? ZortVariationId,
    bool IsActiveFromZort,
    string? RawZortJson);
