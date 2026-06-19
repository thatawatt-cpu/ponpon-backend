using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.FlashSales;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class FlashSaleRepository : IFlashSaleRepository
{
    private readonly CatalogDbContext _dbContext;

    public FlashSaleRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<FlashSale>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.FlashSales
            .AsNoTracking()
            .Include(x => x.Products)
            .OrderByDescending(x => x.StartDate)
            .ToArrayAsync(cancellationToken);
    }

    public Task<FlashSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.FlashSales
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<FlashSale?> GetActiveAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        return _dbContext.FlashSales
            .AsNoTracking()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.StartDate <= today && today <= x.EndDate, cancellationToken);
    }

    public async Task AddAsync(FlashSale flashSale, CancellationToken cancellationToken = default)
    {
        await _dbContext.FlashSales.AddAsync(flashSale, cancellationToken);
    }

    public async Task DeleteProductsAsync(Guid flashSaleId, CancellationToken cancellationToken = default)
    {
        await _dbContext.FlashSaleProducts.Where(x => x.FlashSaleId == flashSaleId).ExecuteDeleteAsync(cancellationToken);

        var tracked = _dbContext.ChangeTracker.Entries<FlashSaleProduct>()
            .Where(e => e.Entity.FlashSaleId == flashSaleId)
            .ToList();

        foreach (var entry in tracked)
            entry.State = EntityState.Detached;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _dbContext.FlashSales.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}
