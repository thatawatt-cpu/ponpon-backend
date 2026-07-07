using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.SyncRuns;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ProductSyncRunRepository : IProductSyncRunRepository
{
    private readonly CatalogDbContext _dbContext;

    public ProductSyncRunRepository(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductSyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.ProductSyncRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ProductSyncRun syncRun, CancellationToken cancellationToken = default)
        => await _dbContext.ProductSyncRuns.AddAsync(syncRun, cancellationToken);
}
