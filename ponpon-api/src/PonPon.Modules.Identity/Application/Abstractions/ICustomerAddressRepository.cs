using PonPon.Modules.Identity.Domain.Customers;

namespace PonPon.Modules.Identity.Application.Abstractions;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyCollection<CustomerAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerAddress?> GetByIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerAddress?> GetDefaultAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);
}
