using PonPon.Modules.Ordering.Domain.Returns;

namespace PonPon.Modules.Ordering.Application.Abstractions;

public interface IOrderReturnRequestRepository
{
    Task<OrderReturnRequest?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrderReturnRequest returnRequest,
        CancellationToken cancellationToken = default);
}
