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
    Task<bool> TryReserveQuotaAsync(
        Guid orderId,
        Guid flashSaleId,
        IReadOnlyDictionary<Guid, int> productQuantities,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);
    Task ReleaseQuotaByOrderAsync(Guid orderId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
