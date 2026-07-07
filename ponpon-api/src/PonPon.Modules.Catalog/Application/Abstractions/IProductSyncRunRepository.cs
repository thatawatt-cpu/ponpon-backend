using PonPon.Modules.Catalog.Domain.SyncRuns;

namespace PonPon.Modules.Catalog.Application.Abstractions;

public interface IProductSyncRunRepository
{
    Task<ProductSyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ProductSyncRun syncRun, CancellationToken cancellationToken = default);
}
