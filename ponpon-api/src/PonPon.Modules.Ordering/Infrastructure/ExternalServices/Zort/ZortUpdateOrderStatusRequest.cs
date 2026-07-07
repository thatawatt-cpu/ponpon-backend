using System.Text.Json.Serialization;

namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed record ZortUpdateOrderStatusRequest(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("status")] int Status);
