using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

namespace PonPon.Modules.Catalog.Tests;

public sealed class ZortProductSnapshotComparerTests
{
    public void SameSnapshotWithDifferentJsonPropertyOrderIsUnchanged()
    {
        var now = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc);
        var product = Product.CreateFromZort(CreateSnapshot(rawJson: """{"id":1,"tag":["Lineliff"],"name":"Tea"}"""), now);
        var snapshot = CreateSnapshot(rawJson: """{"name":"Tea","tag":["Lineliff"],"id":1}""");

        var changed = ZortProductSnapshotComparer.HasChanged(product, snapshot);

        AssertEqual(false, changed);
    }

    public void ChangedStockIsChanged()
    {
        var now = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc);
        var product = Product.CreateFromZort(CreateSnapshot(stock: 10), now);
        var snapshot = CreateSnapshot(stock: 9);

        var changed = ZortProductSnapshotComparer.HasChanged(product, snapshot);

        AssertEqual(true, changed);
    }

    private static ProductSnapshot CreateSnapshot(int stock = 10, string rawJson = """{"id":1,"tag":["Lineliff"]}""")
    {
        return new ProductSnapshot(
            ZortProductId: 1,
            ProductType: 0,
            Name: "Tea",
            Description: null,
            Sku: "SKU-1",
            Barcode: null,
            SellPrice: 100,
            SellVatStatus: 0,
            PurchasePrice: null,
            PurchaseVatStatus: 0,
            Stock: stock,
            AvailableStock: stock,
            UnitText: null,
            ImageUrl: null,
            Weight: null,
            Height: null,
            Length: null,
            Width: null,
            ZortCategoryId: null,
            CategoryName: null,
            ZortSubCategoryId: null,
            SubCategoryName: null,
            ZortVariationId: null,
            IsActiveFromZort: true,
            RawZortJson: rawJson);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
        }
    }
}
