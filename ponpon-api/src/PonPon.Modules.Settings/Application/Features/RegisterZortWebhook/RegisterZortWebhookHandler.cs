using Microsoft.Extensions.Configuration;
using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Settings.Application.Features.RegisterZortWebhook;

public sealed class RegisterZortWebhookHandler
{
    private const string Group = "Zort";

    private readonly IZortWebhookRegistrar _registrar;
    private readonly ISettingsRepository _settings;
    private readonly IConfiguration _configuration;

    public RegisterZortWebhookHandler(
        IZortWebhookRegistrar registrar,
        ISettingsRepository settings,
        IConfiguration configuration)
    {
        _registrar = registrar;
        _settings = settings;
        _configuration = configuration;
    }

    public async Task HandleAsync(CancellationToken cancellationToken = default)
    {
        var settings = (await _settings.GetByGroupAsync(Group, cancellationToken))
            .ToDictionary(x => x.Key, x => x.Value);
        var baseUrl = ResolveValue(settings, "WebhookBaseUrl");
        var key1 = ResolveValue(settings, "WebhookKey1");
        var key2 = ResolveValue(settings, "WebhookKey2");
        var key3 = ResolveValue(settings, "WebhookKey3");

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(key1))
        {
            throw new BadRequestException("ZORT webhook settings are not configured.");
        }

        await _registrar.RegisterAsync(baseUrl, key1, key2, key3, cancellationToken);
    }

    private string? ResolveValue(IReadOnlyDictionary<string, string?> settings, string key)
        => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : _configuration[$"{Group}:{key}"];
}
