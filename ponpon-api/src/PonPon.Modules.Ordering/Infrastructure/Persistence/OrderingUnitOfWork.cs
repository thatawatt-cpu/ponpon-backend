using PonPon.Modules.Ordering.Application.Abstractions;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence;

public sealed class OrderingUnitOfWork : IOrderingUnitOfWork
{
    private readonly OrderingDbContext _dbContext;

    public OrderingUnitOfWork(OrderingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
