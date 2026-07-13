using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Modules.Settings.Domain;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Settings.Application.Features.IntegrationSettings;

public sealed class UpdateIntegrationSettingsHandler
{
    private const string SecretMask = "********";

    private readonly ISettingsRepository _settings;
    private readonly ISettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateIntegrationSettingsHandler(
        ISettingsRepository settings,
        ISettingsUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(
        string group,
        UpdateIntegrationSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var definition = IntegrationSettingsCatalog.FindGroup(group)
            ?? throw new BadRequestException("Integration settings group is not supported.");
        var allowed = definition.Fields.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, rawValue) in request.Values)
        {
            if (!allowed.TryGetValue(key, out var field))
                throw new BadRequestException($"Setting '{key}' is not supported for {definition.Group}.");
            if (field.IsSecret && rawValue == SecretMask)
                continue;

            await UpsertAsync(definition.Group, field.Key, Normalize(rawValue), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(
        string group,
        string key,
        string? value,
        CancellationToken cancellationToken)
    {
        var existing = await _settings.GetAsync(group, key, cancellationToken);
        if (existing is null)
            await _settings.AddAsync(Setting.Create(group, key, value, _clock.UtcNow), cancellationToken);
        else
            existing.Update(value, _clock.UtcNow);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
