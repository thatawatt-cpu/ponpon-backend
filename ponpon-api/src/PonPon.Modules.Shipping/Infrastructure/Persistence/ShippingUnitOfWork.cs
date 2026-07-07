using PonPon.Modules.Shipping.Application.Abstractions;

namespace PonPon.Modules.Shipping.Infrastructure.Persistence;

public sealed class ShippingUnitOfWork : IShippingUnitOfWork
{
    private readonly ShippingDbContext _dbContext;

    public ShippingUnitOfWork(ShippingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
