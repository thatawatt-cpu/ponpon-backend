using PonPon.Modules.Settings.Application.Abstractions;

namespace PonPon.Modules.Settings.Infrastructure.Persistence;

public sealed class SettingsUnitOfWork : ISettingsUnitOfWork
{
    private readonly SettingsDbContext _dbContext;

    public SettingsUnitOfWork(SettingsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
