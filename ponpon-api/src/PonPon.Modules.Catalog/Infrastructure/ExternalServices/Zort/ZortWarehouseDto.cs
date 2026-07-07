using System.Text.Json.Serialization;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortWarehouseDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("address")] string? Address);
