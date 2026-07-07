using PonPon.Modules.Settings.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.GetSettings;

public sealed class GetSettingsHandler
{
    private readonly ISettingsRepository _settings;

    public GetSettingsHandler(ISettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<IReadOnlyCollection<SettingResponse>> HandleAsync(
        GetSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var settings = string.IsNullOrWhiteSpace(query.Group)
            ? await _settings.GetAllAsync(cancellationToken)
            : await _settings.GetByGroupAsync(query.Group, cancellationToken);

        return settings
            .Select(s => new SettingResponse(s.Group, s.Key, s.Value))
            .ToArray();
    }
}
