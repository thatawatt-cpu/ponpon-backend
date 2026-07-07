using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IdentityDbContext _dbContext;

    public CustomerRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Customers
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Customer?> GetByLineUserIdAsync(string lineUserId, CancellationToken cancellationToken = default) => _dbContext.Customers.FirstOrDefaultAsync(x => x.LineProfile.LineUserId == lineUserId, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) => await _dbContext.Customers.AddAsync(customer, cancellationToken);
}
