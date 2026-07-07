using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderingDbContext _dbContext;

    public OrderRepository(OrderingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IOrderPaymentLock> AcquirePaymentLockAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var bytes = orderId.ToByteArray();
        var key1 = BitConverter.ToInt32(bytes, 0);
        var key2 = BitConverter.ToInt32(bytes, 4);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({key1}, {key2})",
            cancellationToken);
        return new OrderPaymentLock(transaction);
    }

    public async Task<IReadOnlyCollection<AdminOrderListItem>> GetAsync(
        string? keyword,
        string? status,
        string? paymentStatus,
        string? returnRequestStatus,
        string? refundRequestStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x =>
                x.Number.Contains(value)
                || (x.CustomerName != null && x.CustomerName.Contains(value))
                || (x.CustomerPhone != null && x.CustomerPhone.Contains(value))
                || (x.TrackingNo != null && x.TrackingNo.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(x => x.PaymentStatus == paymentStatus);
        }

        if (!string.IsNullOrWhiteSpace(returnRequestStatus))
        {
            query = query.Where(order => _dbContext.OrderReturnRequests.Any(
                request => request.OrderId == order.Id
                           && request.Status == returnRequestStatus));
        }

        if (!string.IsNullOrWhiteSpace(refundRequestStatus))
        {
            query = query.Where(x => x.OmiseRefundStatus == refundRequestStatus);
        }

        var orders = await query
            .OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Id).ToArray();
        var returnStatuses = await _dbContext.OrderReturnRequests
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .Select(x => new { x.OrderId, x.Status })
            .ToDictionaryAsync(x => x.OrderId, x => x.Status, cancellationToken);

        return orders
            .Select(x => new AdminOrderListItem(
                x,
                returnStatuses.GetValueOrDefault(x.Id)))
            .ToArray();
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Order?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Number == number, cancellationToken);
    }

    public async Task ReloadAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Entry(order).ReloadAsync(cancellationToken);
        await _dbContext.Entry(order).Collection(x => x.Items).LoadAsync(cancellationToken);
    }

    public async Task<CustomerOrderListProjection> GetCustomerOrdersAsync(
        Guid customerId,
        IReadOnlyCollection<string>? statuses,
        IReadOnlyCollection<string>? paymentStatuses,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId);

        if (statuses is { Count: > 0 })
            query = query.Where(x => statuses.Contains(x.Status));

        if (paymentStatuses is { Count: > 0 })
            query = query.Where(x => paymentStatuses.Contains(x.PaymentStatus));

        var total = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToArray();

        var allItems = await _dbContext.Set<OrderItem>()
            .AsNoTracking()
            .Where(i => orderIds.Contains(i.OrderId))
            .OrderBy(i => i.OrderId)
            .ThenBy(i => i.Id)
            .ToArrayAsync(cancellationToken);

        var itemsByOrder = allItems
            .GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var items = orders.Select(o => new CustomerOrderItem(
            o,
            itemsByOrder.TryGetValue(o.Id, out var oi) ? oi.Length : 0,
            itemsByOrder.TryGetValue(o.Id, out var oi2) ? (IReadOnlyList<OrderItem>)oi2.Take(3).ToArray() : []
        )).ToArray();

        return new CustomerOrderListProjection(items, total);
    }

    public Task<Order?> GetCustomerOrderByIdAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.CustomerId == customerId,
                cancellationToken);
    }

    public Task<Order?> GetByZortOrderIdAsync(long zortOrderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.ZortOrderId == zortOrderId, cancellationToken);
    }

    public Task<int> CountCustomerCompletedOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.AsNoTracking().CountAsync(
            x => x.CustomerId == customerId
                 && x.Status != "Voided"
                 && (x.PaymentStatus == "Paid" || x.PaymentStatus == "1"),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Order>> GetPendingZortSyncAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Where(x => x.ZortOrderId < 0
                     && x.Status != "Voided"
                     && x.SalesChannel == "LineLiff")
            .OrderBy(x => x.CreatedAtUtc)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Order>> GetExpiredUnpaidAsync(DateTime now, string salesChannel, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Where(x => x.SalesChannel == salesChannel
                     && x.PaymentExpiresAt != null
                     && x.PaymentExpiresAt < now
                     && x.PaymentStatus != "Paid"
                     && x.PaymentStatus != "Voided"
                     && x.Status != "Voided")
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> UpdatePaymentStatusAsync(
        Guid orderId,
        string expectedOmiseChargeId,
        string orderStatus,
        string paymentStatus,
        decimal paymentAmount,
        CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Orders
            .Where(x => x.Id == orderId && x.OmiseChargeId == expectedOmiseChargeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, orderStatus)
                .SetProperty(x => x.PaymentStatus, paymentStatus)
                .SetProperty(x => x.PaymentAmount, paymentAmount)
                .SetProperty(x => x.PaymentExpiresAt, (DateTime?)null)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken);
        return affected == 1;
    }
}

internal sealed class OrderPaymentLock : IOrderPaymentLock
{
    private readonly IDbContextTransaction _transaction;
    private bool _completed;

    public OrderPaymentLock(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
            await _transaction.RollbackAsync();

        await _transaction.DisposeAsync();
    }
}
