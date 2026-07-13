using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Settings.Application;

public sealed class RuntimeSettingProvider : IRuntimeSettingProvider
{
    private readonly ISettingsRepository _settings;

    public RuntimeSettingProvider(ISettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<string?> GetValueAsync(
        string group,
        string key,
        CancellationToken cancellationToken = default)
    {
        var setting = await _settings.GetAsync(group, key, cancellationToken);
        return string.IsNullOrWhiteSpace(setting?.Value) ? null : setting.Value;
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetByGroupAsync(group, cancellationToken);
        return settings.ToDictionary(x => x.Key, x => x.Value);
    }
}
