using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Modules.Settings.Domain;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.UpsertSetting;

public sealed class UpsertSettingHandler
{
    private readonly ISettingsRepository _settings;
    private readonly ISettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpsertSettingHandler(
        ISettingsRepository settings,
        ISettingsUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(UpsertSettingCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _settings.GetAsync(command.Group, command.Key, cancellationToken);
        if (existing is null)
        {
            var setting = Setting.Create(command.Group, command.Key, command.Value, _clock.UtcNow);
            await _settings.AddAsync(setting, cancellationToken);
        }
        else
        {
            existing.Update(command.Value, _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
