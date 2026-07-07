using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed class ZortGetWebhookResponse
{
    [JsonPropertyName("addorderurl")] public string? AddOrderUrl { get; set; }
    [JsonPropertyName("updateorderurl")] public string? UpdateOrderUrl { get; set; }
    [JsonPropertyName("deleteorderurl")] public string? DeleteOrderUrl { get; set; }
    [JsonPropertyName("updateordertrackingurl")] public string? UpdateOrderTrackingUrl { get; set; }
    [JsonPropertyName("updateorderpaymenturl")] public string? UpdateOrderPaymentUrl { get; set; }
    [JsonPropertyName("addproducturl")] public string? AddProductUrl { get; set; }
    [JsonPropertyName("updateproducturl")] public string? UpdateProductUrl { get; set; }
    [JsonPropertyName("deleteproducturl")] public string? DeleteProductUrl { get; set; }
    [JsonPropertyName("updatequantityurl")] public string? UpdateQuantityUrl { get; set; }
    [JsonPropertyName("key1")] public string? Key1 { get; set; }
    [JsonPropertyName("key2")] public string? Key2 { get; set; }
    [JsonPropertyName("key3")] public string? Key3 { get; set; }
}
