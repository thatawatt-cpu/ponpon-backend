using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PonPon.Modules.Ordering.Application;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Returns;

namespace PonPon.Modules.Ordering.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private const string AnyReturnOrRefundStatus = "__any_return_or_refund__";

    private readonly OrderingDbContext _dbContext;

    public OrderRepository(OrderingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IOrderPaymentLock> AcquirePaymentLockAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => await AcquireAdvisoryLockAsync(orderId, cancellationToken);

    public async Task<IOrderPaymentLock> AcquireClientRequestLockAsync(
        Guid clientRequestId,
        CancellationToken cancellationToken = default)
        => await AcquireAdvisoryLockAsync(clientRequestId, cancellationToken);

    private async Task<IOrderPaymentLock> AcquireAdvisoryLockAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var bytes = id.ToByteArray();
        var key1 = BitConverter.ToInt32(bytes, 0);
        var key2 = BitConverter.ToInt32(bytes, 4);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({key1}, {key2})",
            cancellationToken);
        return new OrderPaymentLock(transaction);
    }

    public async Task<AdminOrderListProjection> GetAsync(
        string? keyword,
        string? status,
        string? paymentStatus,
        string? returnRequestStatus,
        string? refundRequestStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? shippingChannel,
        string? salesChannel,
        string? sortBy,
        string? sortDirection,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            var pattern = $"%{value}%";
            baseQuery = baseQuery.Where(x =>
                EF.Functions.ILike(x.Number, pattern)
                || (x.CustomerName != null && EF.Functions.ILike(x.CustomerName, pattern))
                || (x.CustomerPhone != null && EF.Functions.ILike(x.CustomerPhone, pattern))
                || (x.TrackingNo != null && EF.Functions.ILike(x.TrackingNo, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            baseQuery = baseQuery.Where(x => x.PaymentStatus == paymentStatus);
        }

        if (dateFrom.HasValue)
        {
            baseQuery = baseQuery.Where(x => (x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc) >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            baseQuery = baseQuery.Where(x => (x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc) <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(shippingChannel))
        {
            baseQuery = baseQuery.Where(x => x.ShippingChannel == shippingChannel);
        }

        if (!string.IsNullOrWhiteSpace(salesChannel))
        {
            baseQuery = baseQuery.Where(x => x.SalesChannel == salesChannel);
        }

        if (IsReturnOrRefundAnyFilter(returnRequestStatus, refundRequestStatus))
        {
            baseQuery = baseQuery.Where(order =>
                order.Status == "Returned"
                || order.Status == ((int)ZortOrderStatus.Returned).ToString()
                || !string.IsNullOrWhiteSpace(order.OmiseRefundStatus)
                || _dbContext.OrderReturnRequests.Any(request => request.OrderId == order.Id));
        }
        else if (IsReturnOrRefundRequestedFilter(returnRequestStatus, refundRequestStatus))
        {
            baseQuery = baseQuery.Where(order =>
                _dbContext.OrderReturnRequests.Any(
                    request => request.OrderId == order.Id
                               && request.Status == returnRequestStatus)
                || order.OmiseRefundStatus == refundRequestStatus);
        }
        else if (!string.IsNullOrWhiteSpace(returnRequestStatus))
        {
            baseQuery = baseQuery.Where(order => _dbContext.OrderReturnRequests.Any(
                request => request.OrderId == order.Id
                           && request.Status == returnRequestStatus));
        }

        if (!string.IsNullOrWhiteSpace(refundRequestStatus))
        {
            baseQuery = baseQuery.Where(x => x.OmiseRefundStatus == refundRequestStatus);
        }

        if (ShouldExcludeReturnOrRefundWorkflow(status, paymentStatus, returnRequestStatus, refundRequestStatus))
        {
            baseQuery = baseQuery.Where(order =>
                !_dbContext.OrderReturnRequests.Any(request => request.OrderId == order.Id)
                && string.IsNullOrWhiteSpace(order.OmiseRefundStatus));
        }

        var statusCounts = await baseQuery
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var query = baseQuery;
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);
        var orders = await ApplySort(query, sortBy, sortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Id).ToArray();
        var returnStatuses = await _dbContext.OrderReturnRequests
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .Select(x => new { x.OrderId, x.Status })
            .GroupBy(x => x.OrderId)
            .ToDictionaryAsync(x => x.Key, x => x.First().Status, cancellationToken);

        var items = orders
            .Select(x => new AdminOrderListItem(
                x,
                returnStatuses.GetValueOrDefault(x.Id)))
            .ToArray();

        return new AdminOrderListProjection(items, total, statusCounts);
    }

    private static bool ShouldExcludeReturnOrRefundWorkflow(
        string? status,
        string? paymentStatus,
        string? returnRequestStatus,
        string? refundRequestStatus)
        => (!string.IsNullOrWhiteSpace(status) || !string.IsNullOrWhiteSpace(paymentStatus))
           && string.IsNullOrWhiteSpace(returnRequestStatus)
           && string.IsNullOrWhiteSpace(refundRequestStatus);

    private static bool IsReturnOrRefundRequestedFilter(string? returnRequestStatus, string? refundRequestStatus)
        => string.Equals(returnRequestStatus, OrderReturnRequestStatus.Requested, StringComparison.OrdinalIgnoreCase)
           && string.Equals(refundRequestStatus, OrderRefundStatus.ManualRefundPending, StringComparison.OrdinalIgnoreCase);

    private static bool IsReturnOrRefundAnyFilter(string? returnRequestStatus, string? refundRequestStatus)
        => string.Equals(returnRequestStatus, AnyReturnOrRefundStatus, StringComparison.OrdinalIgnoreCase)
           && string.Equals(refundRequestStatus, AnyReturnOrRefundStatus, StringComparison.OrdinalIgnoreCase);

    private static IOrderedQueryable<Order> ApplySort(
        IQueryable<Order> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy)
            ? "orderDate"
            : sortBy.Trim();

        return normalizedSortBy.ToLowerInvariant() switch
        {
            "number" or "ordernumber" => descending
                ? query.OrderByDescending(x => x.Number)
                : query.OrderBy(x => x.Number),
            "customername" => descending
                ? query.OrderByDescending(x => x.CustomerName)
                : query.OrderBy(x => x.CustomerName),
            "paymentamount" or "total" => descending
                ? query.OrderByDescending(x => x.PaymentAmount)
                : query.OrderBy(x => x.PaymentAmount),
            "status" => descending
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),
            "lastsyncedat" => descending
                ? query.OrderByDescending(x => x.LastSyncedAt)
                : query.OrderBy(x => x.LastSyncedAt),
            "createdat" or "createdatutc" => descending
                ? query.OrderByDescending(x => x.CreatedAtUtc)
                : query.OrderBy(x => x.CreatedAtUtc),
            _ => descending
                ? query.OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
                : query.OrderBy(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
        };
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
        MyOrderFilter? filter,
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

        query = ApplyCustomerFilter(query, filter);

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

        var orderItemIds = allItems.Select(x => x.Id).ToArray();
        var reviewIdsByOrderItem = await _dbContext.ReviewReadModels
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.OrderItemId) && x.DeletedAtUtc == null)
            .Select(x => new { x.OrderItemId, x.Id })
            .ToDictionaryAsync(x => x.OrderItemId, x => (Guid?)x.Id, cancellationToken);

        var itemsByOrder = allItems
            .GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var items = orders.Select(o => new CustomerOrderItem(
            o,
            itemsByOrder.TryGetValue(o.Id, out var oi) ? oi.Length : 0,
            itemsByOrder.TryGetValue(o.Id, out var oi2)
                ? oi2
                    .Take(3)
                    .Select(item => new CustomerOrderItemPreview(
                        item,
                        reviewIdsByOrderItem.GetValueOrDefault(item.Id)))
                    .ToArray()
                : []
        )).ToArray();

        return new CustomerOrderListProjection(items, total);
    }

    private IQueryable<Order> ApplyCustomerFilter(IQueryable<Order> query, MyOrderFilter? filter)
    {
        if (filter is null)
            return query;

        return filter switch
        {
            MyOrderFilter.PendingPayment => query.Where(x =>
                x.PaymentStatus == "Pending"
                || x.PaymentStatus == ((int)ZortPaymentStatus.Pending).ToString()),
            MyOrderFilter.Preparing => query.Where(x =>
                (x.Status == "Waiting"
                    || x.Status == ((int)ZortOrderStatus.Waiting).ToString()
                    || x.Status == "Packed"
                    || x.Status == ((int)ZortOrderStatus.Packed).ToString())
                && (x.PaymentStatus == "Paid"
                    || x.PaymentStatus == ((int)ZortPaymentStatus.Paid).ToString())
                && x.OmiseRefundStatus == null
                && !_dbContext.OrderReturnRequests.Any(request => request.OrderId == x.Id)),
            MyOrderFilter.AwaitingReceive => query.Where(x =>
                x.ReceivedAtUtc == null
                && (x.Status == "Shipping"
                    || x.Status == ((int)ZortOrderStatus.Shipping).ToString()
                    || x.Status == "Success"
                    || x.Status == ((int)ZortOrderStatus.Success).ToString())),
            MyOrderFilter.Completed => query.Where(x => x.ReceivedAtUtc != null),
            MyOrderFilter.Cancelled => query.Where(x =>
                x.Status == "Voided"
                || x.Status == ((int)ZortOrderStatus.Voided).ToString()),
            MyOrderFilter.ReturnRefund => query.Where(x =>
                x.Status == "Returned"
                || x.Status == ((int)ZortOrderStatus.Returned).ToString()
                || x.OmiseRefundStatus != null
                || _dbContext.OrderReturnRequests.Any(request => request.OrderId == x.Id)),
            MyOrderFilter.AwaitingReview => query.Where(x =>
                x.ReceivedAtUtc != null
                && _dbContext.OrderItems.Any(item =>
                    item.OrderId == x.Id
                    && item.ProductId != null
                    && !_dbContext.ReviewReadModels.Any(review =>
                        review.OrderItemId == item.Id
                        && review.DeletedAtUtc == null))),
            _ => query
        };
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

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetReviewIdsByOrderItemIdsAsync(
        IReadOnlyCollection<Guid> orderItemIds,
        CancellationToken cancellationToken = default)
    {
        if (orderItemIds.Count == 0)
            return new Dictionary<Guid, Guid>();

        return await _dbContext.ReviewReadModels
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.OrderItemId) && x.DeletedAtUtc == null)
            .Select(x => new { x.OrderItemId, x.Id })
            .ToDictionaryAsync(x => x.OrderItemId, x => x.Id, cancellationToken);
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
                     && (x.PaymentStatus == "Paid" || x.PaymentStatus == "1")
                     && x.SalesChannel == "LineLiff")
            .OrderBy(x => x.CreatedAtUtc)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Order>> GetDeliveredUnreceivedOlderThanAsync(
        DateTime cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Where(x => x.ReceivedAtUtc == null
                     && (x.Status == "Success" || x.Status == ((int)ZortOrderStatus.Success).ToString())
                     && x.LastSyncedAt <= cutoffUtc)
            .OrderBy(x => x.LastSyncedAt)
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

    public async Task<bool> TryMarkStockReleasedAsync(
        Guid orderId,
        DateTime releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Orders
            .Where(x => x.Id == orderId && x.HasStockReservation)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.HasStockReservation, false)
                .SetProperty(x => x.UpdatedAtUtc, releasedAtUtc),
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
