using PonPon.Modules.Settings.Domain;

namespace PonPon.Modules.Settings.Application.Abstractions;

public interface ISettingsRepository
{
    Task<IReadOnlyCollection<Setting>> GetByGroupAsync(string group, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Setting?> GetAsync(string group, string key, CancellationToken cancellationToken = default);
    Task AddAsync(Setting setting, CancellationToken cancellationToken = default);
}
