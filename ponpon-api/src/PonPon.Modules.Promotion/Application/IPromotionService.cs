using PonPon.Modules.Promotion.Domain;
using PromotionEntity = PonPon.Modules.Promotion.Domain.Promotion;

namespace PonPon.Modules.Promotion.Application;

public interface IPromotionService
{
    Task<IReadOnlyCollection<PromotionEntity>> GetAllAsync(Guid? campaignId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PromotionEntity>> GetActiveAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<PromotionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PromotionUsage>> GetUsagesAsync(Guid promotionId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(PromotionInput input, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, PromotionInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetActiveCustomerUsageCountAsync(Guid promotionId, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> TryReserveAsync(Guid promotionId, Guid orderId, Guid customerId, CancellationToken cancellationToken = default, decimal discountAmount = 0);
    Task ReleaseByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
