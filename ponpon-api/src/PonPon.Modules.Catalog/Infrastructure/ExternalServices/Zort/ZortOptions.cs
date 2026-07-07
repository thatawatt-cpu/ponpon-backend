namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed class ZortOptions
{
    public string BaseUrl { get; init; } = "https://open-api.zortout.com/v4";
    public string StoreName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public int DefaultPageLimit { get; init; } = 100;
    public string WarehouseCode { get; init; } = string.Empty;
    public string WebhookKey { get; init; } = string.Empty;
}
