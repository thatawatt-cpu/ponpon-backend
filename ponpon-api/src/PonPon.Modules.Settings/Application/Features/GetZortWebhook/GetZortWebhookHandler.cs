using PonPon.Modules.Settings.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.GetZortWebhook;

public sealed class GetZortWebhookHandler
{
    private const string Group = "Zort";

    private readonly ISettingsRepository _settings;

    public GetZortWebhookHandler(ISettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<GetZortWebhookResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetByGroupAsync(Group, cancellationToken);
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);

        return new GetZortWebhookResponse(
            dict.GetValueOrDefault("WebhookBaseUrl"),
            dict.GetValueOrDefault("WebhookKey1"),
            dict.GetValueOrDefault("WebhookKey2"),
            dict.GetValueOrDefault("WebhookKey3"));
    }
}
