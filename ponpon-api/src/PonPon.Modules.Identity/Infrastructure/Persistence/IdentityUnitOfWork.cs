using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityUnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _dbContext;

    public IdentityUnitOfWork(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
