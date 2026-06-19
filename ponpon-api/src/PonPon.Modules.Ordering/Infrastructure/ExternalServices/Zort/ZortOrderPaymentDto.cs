using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed class ZortOrderPaymentDto
{
    [JsonPropertyName("id")] public JsonElement Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("amount")] public JsonElement Amount { get; set; }
    [JsonPropertyName("paymentdatetime")] public JsonElement PaymentDateTime { get; set; }
    [JsonPropertyName("paymentdatetimeString")] public string? PaymentDateTimeString { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
