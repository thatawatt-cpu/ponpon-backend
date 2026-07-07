using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed record ZortRegisterWebhookRequest(
    [property: JsonPropertyName("updateorderurl")] string UpdateOrderUrl,
    [property: JsonPropertyName("key1")] string Key1,
    [property: JsonPropertyName("key2")] string? Key2,
    [property: JsonPropertyName("key3")] string? Key3);
