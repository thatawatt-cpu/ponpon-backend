using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed class ZortProductVariantDto
{
    [JsonPropertyName("variantid")]
    public JsonElement VariantId { get; set; }

    [JsonPropertyName("variantname")]
    public string? VariantName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
