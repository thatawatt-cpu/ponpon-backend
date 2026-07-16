using Microsoft.EntityFrameworkCore;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class OrderUsageReadService(OrderingDbContext dbContext) : IOrderUsageReadService
{
    public async Task<IReadOnlyDictionary<Guid, OrderUsageDetails>> GetByIdsAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken = default)
    {
        var ids = orderIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, OrderUsageDetails>();

        return await dbContext.Orders
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new OrderUsageDetails(
                x.Id,
                x.Number,
                x.CustomerName ?? x.ShippingName,
                x.CustomerPhone ?? x.ShippingPhone,
                x.Amount))
            .ToDictionaryAsync(x => x.OrderId, cancellationToken);
    }
}
