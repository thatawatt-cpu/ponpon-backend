using System.Text.Json.Serialization;

namespace PonPon.Modules.Identity.Infrastructure.Line;

public sealed class LineTokenVerifyResponse
{
    [JsonPropertyName("sub")]
    public string LineUserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("picture")]
    public string? PictureUrl { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
