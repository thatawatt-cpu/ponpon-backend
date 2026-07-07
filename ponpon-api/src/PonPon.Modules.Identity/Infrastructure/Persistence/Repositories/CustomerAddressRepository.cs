using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Repositories;

public sealed class CustomerAddressRepository : ICustomerAddressRepository
{
    private readonly IdentityDbContext _dbContext;

    public CustomerAddressRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyCollection<CustomerAddress>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.CustomerAddresses
            .Where(x => x.CustomerId == customerId && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<CustomerAddress?> GetByIdAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        _dbContext.CustomerAddresses.FirstOrDefaultAsync(
            x => x.Id == id && x.CustomerId == customerId && !x.IsDeleted,
            cancellationToken);

    public Task<bool> HasAnyAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        _dbContext.CustomerAddresses.AnyAsync(x => x.CustomerId == customerId && !x.IsDeleted, cancellationToken);

    public Task<CustomerAddress?> GetDefaultAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        _dbContext.CustomerAddresses.FirstOrDefaultAsync(
            x => x.CustomerId == customerId && x.IsDefault && !x.IsDeleted,
            cancellationToken);

    public async Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default) =>
        await _dbContext.CustomerAddresses.AddAsync(address, cancellationToken);
}
