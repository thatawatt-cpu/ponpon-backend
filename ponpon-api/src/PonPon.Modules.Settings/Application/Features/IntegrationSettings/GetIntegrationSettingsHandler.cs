using Microsoft.Extensions.Configuration;
using PonPon.Modules.Settings.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.IntegrationSettings;

public sealed class GetIntegrationSettingsHandler
{
    private const string SecretMask = "********";

    private readonly ISettingsRepository _settings;
    private readonly IConfiguration _configuration;

    public GetIntegrationSettingsHandler(ISettingsRepository settings, IConfiguration configuration)
    {
        _settings = settings;
        _configuration = configuration;
    }

    public async Task<IntegrationSettingsResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = new List<IntegrationSettingsGroupResponse>();
        foreach (var group in IntegrationSettingsCatalog.Groups)
        {
            var stored = (await _settings.GetByGroupAsync(group.Group, cancellationToken))
                .ToDictionary(x => x.Key, x => x.Value);
            var fields = group.Fields
                .Select(field =>
                {
                    var value = ResolveValue(group.Group, field.Key, stored);
                    var isConfigured = !string.IsNullOrWhiteSpace(value);
                    return new IntegrationSettingFieldResponse(
                        field.Key,
                        field.Label,
                        field.IsSecret,
                        isConfigured,
                        field.IsSecret && isConfigured ? SecretMask : value);
                })
                .ToArray();

            groups.Add(new IntegrationSettingsGroupResponse(group.Group, group.DisplayName, fields));
        }

        return new IntegrationSettingsResponse(groups);
    }

    private string? ResolveValue(
        string group,
        string key,
        IReadOnlyDictionary<string, string?> stored)
        => stored.TryGetValue(key, out var storedValue) && !string.IsNullOrWhiteSpace(storedValue)
            ? storedValue
            : _configuration[$"{group}:{key}"];
}
