using PonPon.Modules.Catalog.Domain.FlashSales;

namespace PonPon.Modules.Catalog.Application.Abstractions;

public interface IFlashSaleRepository
{
    Task<IReadOnlyCollection<FlashSale>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FlashSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FlashSale?> GetActiveAsync(DateOnly today, CancellationToken cancellationToken = default);
    Task AddAsync(FlashSale flashSale, CancellationToken cancellationToken = default);
    Task DeleteProductsAsync(Guid flashSaleId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
