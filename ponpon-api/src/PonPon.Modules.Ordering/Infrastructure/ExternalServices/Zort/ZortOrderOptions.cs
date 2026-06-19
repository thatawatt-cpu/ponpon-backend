namespace PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

public sealed class ZortOrderOptions
{
    public string BaseUrl { get; init; } = "https://open-api.zortout.com/v4";
    public string StoreName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string WebhookKey { get; init; } = string.Empty;
}
