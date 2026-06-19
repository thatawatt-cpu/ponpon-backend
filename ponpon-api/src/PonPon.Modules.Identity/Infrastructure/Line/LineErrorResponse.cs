using System.Text.Json.Serialization;

namespace PonPon.Modules.Identity.Infrastructure.Line;

public sealed class LineErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = string.Empty;
}
