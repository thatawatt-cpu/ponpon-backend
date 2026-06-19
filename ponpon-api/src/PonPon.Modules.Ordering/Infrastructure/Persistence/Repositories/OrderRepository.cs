using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyCollection<Order>> GetAsync(
        string? keyword,
        string? status,
        string? paymentStatus,
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

        return await query
            .OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Order>> GetCustomerOrdersAsync(
        Guid customerId,
        string? status,
        string? paymentStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(x => x.PaymentStatus == paymentStatus);
        }

        return await query
            .OrderByDescending(x => x.OrderDate ?? x.ZortCreatedAt ?? x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Order?> GetCustomerOrderByIdAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .AsNoTracking()
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

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }
}
