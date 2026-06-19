using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed class ZortOrderItemDto
{
    [JsonPropertyName("productid")] public JsonElement ProductId { get; set; }
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("number")] public JsonElement Number { get; set; }
    [JsonPropertyName("unittext")] public string? UnitText { get; set; }
    [JsonPropertyName("pricepernumber")] public JsonElement PricePerNumber { get; set; }
    [JsonPropertyName("discount")] public string? Discount { get; set; }
    [JsonPropertyName("discountamount")] public JsonElement DiscountAmount { get; set; }
    [JsonPropertyName("totalprice")] public JsonElement TotalPrice { get; set; }
    [JsonPropertyName("producttype")] public JsonElement ProductType { get; set; }
    [JsonPropertyName("bundleid")] public JsonElement BundleId { get; set; }
    [JsonPropertyName("bundleCode")] public string? BundleCode { get; set; }
    [JsonPropertyName("bundleName")] public string? BundleName { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
