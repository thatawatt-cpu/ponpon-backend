using PonPon.Modules.Ordering.Domain.SyncRuns;

namespace PonPon.Modules.Ordering.Application.Abstractions;

public interface IOrderSyncRunRepository
{
    Task<OrderSyncRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSuccessfulCompletedAtAsync(CancellationToken cancellationToken = default);
    Task AddAsync(OrderSyncRun syncRun, CancellationToken cancellationToken = default);
}
