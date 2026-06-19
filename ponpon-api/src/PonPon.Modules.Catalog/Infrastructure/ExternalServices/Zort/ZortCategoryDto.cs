using System.Text.Json;
using System.Text.Json.Serialization;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed class ZortCategoryDto
{
    [JsonPropertyName("id")] public JsonElement Id { get; set; }
    [JsonPropertyName("categoryid")] public JsonElement CategoryId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("category")] public string? Category { get; set; }
    [JsonPropertyName("categoryname")] public string? CategoryName { get; set; }
    [JsonPropertyName("active")] public JsonElement Active { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
