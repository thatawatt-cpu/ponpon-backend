using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Returns;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class OrderReturnRequestRepository : IOrderReturnRequestRepository
{
    private readonly OrderingDbContext _dbContext;

    public OrderReturnRequestRepository(OrderingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OrderReturnRequest?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderReturnRequests
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(
        OrderReturnRequest returnRequest,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderReturnRequests.AddAsync(returnRequest, cancellationToken);
    }
}
