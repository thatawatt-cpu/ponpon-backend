using PonPon.Modules.Catalog.Domain.Warehouses;

namespace PonPon.Modules.Catalog.Application.Abstractions;

public interface IWarehouseRepository
{
    Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByZortIdAsync(long zortWarehouseId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Warehouse> warehouses, CancellationToken cancellationToken = default);
}
