using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.SyncRuns;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class OrderSyncRunRepository : IOrderSyncRunRepository
{
    private readonly OrderingDbContext _dbContext;

    public OrderSyncRunRepository(OrderingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OrderSyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.OrderSyncRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(OrderSyncRun syncRun, CancellationToken cancellationToken = default)
        => await _dbContext.OrderSyncRuns.AddAsync(syncRun, cancellationToken);
}
