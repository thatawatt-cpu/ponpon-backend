using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed class ZortProductDto
{
    [JsonPropertyName("id")] public JsonElement Id { get; set; }
    [JsonPropertyName("producttype")] public JsonElement ProductType { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("sellprice")] public JsonElement SellPrice { get; set; }
    [JsonPropertyName("sell_vat_status")] public JsonElement SellVatStatus { get; set; }
    [JsonPropertyName("purchaseprice")] public JsonElement PurchasePrice { get; set; }
    [JsonPropertyName("purchase_vat_status")] public JsonElement PurchaseVatStatus { get; set; }
    [JsonPropertyName("barcode")] public string? Barcode { get; set; }
    [JsonPropertyName("stock")] public JsonElement Stock { get; set; }
    [JsonPropertyName("availablestock")] public JsonElement AvailableStock { get; set; }
    [JsonPropertyName("unittext")] public string? UnitText { get; set; }
    [JsonPropertyName("imagepath")] public string? ImagePath { get; set; }
    [JsonPropertyName("imageList")] public JsonElement ImageList { get; set; }
    [JsonPropertyName("weight")] public JsonElement Weight { get; set; }
    [JsonPropertyName("height")] public JsonElement Height { get; set; }
    [JsonPropertyName("length")] public JsonElement Length { get; set; }
    [JsonPropertyName("width")] public JsonElement Width { get; set; }
    [JsonPropertyName("categoryid")] public JsonElement CategoryId { get; set; }
    [JsonPropertyName("category")] public string? Category { get; set; }
    [JsonPropertyName("subCategoryId")] public JsonElement SubCategoryId { get; set; }
    [JsonPropertyName("subCategory")] public string? SubCategory { get; set; }
    [JsonPropertyName("variationid")] public JsonElement VariationId { get; set; }
    [JsonPropertyName("variant")] public IReadOnlyCollection<ZortProductVariantDto>? Variant { get; set; }
    [JsonPropertyName("tag")] public JsonElement Tag { get; set; }
    [JsonPropertyName("sharelink")] public string? ShareLink { get; set; }
    [JsonPropertyName("active")] public JsonElement Active { get; set; }
    [JsonPropertyName("properties")] public JsonElement Properties { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
