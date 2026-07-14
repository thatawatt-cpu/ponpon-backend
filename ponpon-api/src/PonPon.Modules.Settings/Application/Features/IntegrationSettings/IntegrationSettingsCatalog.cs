namespace PonPon.Modules.Settings.Application.Features.IntegrationSettings;

public static class IntegrationSettingsCatalog
{
    public static readonly IReadOnlyCollection<IntegrationSettingsGroupDefinition> Groups =
    [
        new("Line", "LINE",
        [
            new("ChannelId", "Channel ID", false),
            new("ChannelSecret", "Channel secret", true),
            new("ChannelAccessToken", "Channel access token", true),
            new("AdminRecipientIds", "Admin recipient IDs", false),
            new("WebAppBaseUrl", "Web app base URL", false)
        ]),
        new("Zort", "ZORT",
        [
            new("BaseUrl", "Base URL", false),
            new("StoreName", "Store name", false),
            new("ApiKey", "API key", true),
            new("ApiSecret", "API secret", true),
            new("WarehouseCode", "Warehouse code", false),
            new("WebhookBaseUrl", "Webhook base URL", false),
            new("WebhookKey1", "Webhook key 1", true),
            new("WebhookKey2", "Webhook key 2", true),
            new("WebhookKey3", "Webhook key 3", true)
        ]),
        new("Shippop", "SHIPPOP",
        [
            new("ApiKey", "API key", true),
            new("BaseUrl", "Base URL", false)
        ]),
        new("Omise", "OMISE",
        [
            new("PublicKey", "Public key", false),
            new("SecretKey", "Secret key", true)
        ]),
        new("Supabase", "SUPABASE",
        [
            new("Url", "URL", false),
            new("ServiceRoleKey", "Service role key", true),
            new("StorageBucket", "Storage bucket", false)
        ])
    ];

    public static IntegrationSettingsGroupDefinition? FindGroup(string group)
        => Groups.FirstOrDefault(x => string.Equals(x.Group, group, StringComparison.OrdinalIgnoreCase));
}

public sealed record IntegrationSettingsGroupDefinition(
    string Group,
    string DisplayName,
    IReadOnlyCollection<IntegrationSettingFieldDefinition> Fields);

public sealed record IntegrationSettingFieldDefinition(
    string Key,
    string Label,
    bool IsSecret);
