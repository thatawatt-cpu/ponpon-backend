using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Warehouses;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly CatalogDbContext _dbContext;

    public WarehouseRepository(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Warehouses.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

    public Task<Warehouse?> GetByZortIdAsync(long zortWarehouseId, CancellationToken cancellationToken = default)
        => _dbContext.Warehouses
            .FirstOrDefaultAsync(x => x.ZortWarehouseId == zortWarehouseId, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Warehouse> warehouses, CancellationToken cancellationToken = default)
        => await _dbContext.Warehouses.AddRangeAsync(warehouses, cancellationToken);
}
