using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Modules.Settings.Domain;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.RegisterZortWebhook;

public sealed class RegisterZortWebhookHandler
{
    private const string Group = "Zort";

    private readonly IZortWebhookRegistrar _registrar;
    private readonly ISettingsRepository _settings;
    private readonly ISettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public RegisterZortWebhookHandler(
        IZortWebhookRegistrar registrar,
        ISettingsRepository settings,
        ISettingsUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _registrar = registrar;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(RegisterZortWebhookCommand command, CancellationToken cancellationToken = default)
    {
        await _registrar.RegisterAsync(command.BaseUrl, command.Key1, command.Key2, command.Key3, cancellationToken);

        await UpsertAsync("WebhookBaseUrl", command.BaseUrl, cancellationToken);
        await UpsertAsync("WebhookKey1", command.Key1, cancellationToken);
        await UpsertAsync("WebhookKey2", command.Key2, cancellationToken);
        await UpsertAsync("WebhookKey3", command.Key3, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(string key, string? value, CancellationToken cancellationToken)
    {
        var existing = await _settings.GetAsync(Group, key, cancellationToken);
        if (existing is null)
            await _settings.AddAsync(Setting.Create(Group, key, value, _clock.UtcNow), cancellationToken);
        else
            existing.Update(value, _clock.UtcNow);
    }
}
