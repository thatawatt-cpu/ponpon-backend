using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Application.Abstractions;

public interface IOrderRepository
{
    Task<IReadOnlyCollection<Order>> GetAsync(
        string? keyword,
        string? status,
        string? paymentStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> GetCustomerOrdersAsync(
        Guid customerId,
        string? status,
        string? paymentStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Order?> GetCustomerOrderByIdAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task<Order?> GetByZortOrderIdAsync(long zortOrderId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
