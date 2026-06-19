using System.Text.Json;
using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public static class ZortProductSnapshotComparer
{
    public static bool HasChanged(ProductVariant variant, ProductSnapshot snapshot)
    {
        var fullSku = string.IsNullOrWhiteSpace(snapshot.Sku) ? $"ZORT-{snapshot.ZortProductId}" : snapshot.Sku;
        return variant.ZortProductId != snapshot.ZortProductId
               || variant.ZortVariationId != snapshot.ZortVariationId
               || variant.Sku != fullSku
               || variant.Barcode != snapshot.Barcode
               || variant.SellPrice != snapshot.SellPrice
               || variant.SellVatStatus != snapshot.SellVatStatus
               || variant.PurchasePrice != snapshot.PurchasePrice
               || variant.PurchaseVatStatus != snapshot.PurchaseVatStatus
               || variant.Stock != snapshot.Stock
               || variant.AvailableStock != snapshot.AvailableStock
               || variant.UnitText != snapshot.UnitText
               || variant.ImageUrl != snapshot.ImageUrl
               || variant.IsActiveFromZort != snapshot.IsActiveFromZort
               || variant.Status != (snapshot.IsActiveFromZort ? ProductStatus.Active : ProductStatus.Inactive)
               || !JsonEquals(variant.RawZortJson, snapshot.RawZortJson);
    }

    public static bool HasChanged(Product product, ProductSnapshot snapshot)
    {
        var parsedSku = Product.ParseSku(snapshot.Sku);
        return product.ZortProductId != snapshot.ZortProductId
               || product.BaseSku != parsedSku.BaseSku
               || product.ProductType != snapshot.ProductType
               || product.Name != NormalizeName(snapshot.Name)
               || product.Description != snapshot.Description
               || product.Barcode != snapshot.Barcode
               || product.SellPrice != snapshot.SellPrice
               || product.SellVatStatus != snapshot.SellVatStatus
               || product.PurchasePrice != snapshot.PurchasePrice
               || product.PurchaseVatStatus != snapshot.PurchaseVatStatus
               || product.Stock != snapshot.Stock
               || product.AvailableStock != snapshot.AvailableStock
               || product.UnitText != snapshot.UnitText
               || product.ImageUrl != snapshot.ImageUrl
               || product.Weight != snapshot.Weight
               || product.Height != snapshot.Height
               || product.Length != snapshot.Length
               || product.Width != snapshot.Width
               || product.ZortCategoryId != snapshot.ZortCategoryId
               || product.CategoryName != snapshot.CategoryName
               || product.ZortSubCategoryId != snapshot.ZortSubCategoryId
               || product.SubCategoryName != snapshot.SubCategoryName
               || product.ZortVariationId != snapshot.ZortVariationId
               || product.IsActiveFromZort != snapshot.IsActiveFromZort
               || product.Status != (snapshot.IsActiveFromZort ? ProductStatus.Active : ProductStatus.Inactive)
               || !JsonEquals(product.RawZortJson, snapshot.RawZortJson);
    }

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Untitled product" : name;
    }

    private static bool JsonEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            using var leftDocument = JsonDocument.Parse(left);
            using var rightDocument = JsonDocument.Parse(right);
            return JsonElementEquals(leftDocument.RootElement, rightDocument.RootElement);
        }
        catch (JsonException)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }

    private static bool JsonElementEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.Object => ObjectEquals(left, right),
            JsonValueKind.Array => ArrayEquals(left, right),
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.GetRawText() == right.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            _ => left.GetRawText() == right.GetRawText()
        };
    }

    private static bool ObjectEquals(JsonElement left, JsonElement right)
    {
        var leftProperties = left.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
        var rightProperties = right.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
        if (leftProperties.Length != rightProperties.Length)
        {
            return false;
        }

        for (var i = 0; i < leftProperties.Length; i++)
        {
            if (leftProperties[i].Name != rightProperties[i].Name
                || !JsonElementEquals(leftProperties[i].Value, rightProperties[i].Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ArrayEquals(JsonElement left, JsonElement right)
    {
        var leftItems = left.EnumerateArray().ToArray();
        var rightItems = right.EnumerateArray().ToArray();
        if (leftItems.Length != rightItems.Length)
        {
            return false;
        }

        for (var i = 0; i < leftItems.Length; i++)
        {
            if (!JsonElementEquals(leftItems[i], rightItems[i]))
            {
                return false;
            }
        }

        return true;
    }
}
