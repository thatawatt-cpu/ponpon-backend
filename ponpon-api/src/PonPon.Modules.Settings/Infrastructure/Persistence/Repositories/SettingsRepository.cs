using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Settings.Application.Abstractions;
using PonPon.Modules.Settings.Domain;

namespace PonPon.Modules.Settings.Infrastructure.Persistence.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly SettingsDbContext _dbContext;

    public SettingsRepository(SettingsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Settings.AsNoTracking()
            .OrderBy(x => x.Group).ThenBy(x => x.Key)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Setting>> GetByGroupAsync(string group, CancellationToken cancellationToken = default)
        => await _dbContext.Settings.AsNoTracking()
            .Where(x => x.Group == group)
            .OrderBy(x => x.Key)
            .ToArrayAsync(cancellationToken);

    public Task<Setting?> GetAsync(string group, string key, CancellationToken cancellationToken = default)
        => _dbContext.Settings
            .FirstOrDefaultAsync(x => x.Group == group && x.Key == key, cancellationToken);

    public async Task AddAsync(Setting setting, CancellationToken cancellationToken = default)
        => await _dbContext.Settings.AddAsync(setting, cancellationToken);
}
