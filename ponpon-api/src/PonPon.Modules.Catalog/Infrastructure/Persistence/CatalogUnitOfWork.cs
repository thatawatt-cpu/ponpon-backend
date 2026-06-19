using PonPon.Modules.Catalog.Application.Abstractions;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence;

public sealed class CatalogUnitOfWork : ICatalogUnitOfWork
{
    private readonly CatalogDbContext _dbContext;

    public CatalogUnitOfWork(CatalogDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
