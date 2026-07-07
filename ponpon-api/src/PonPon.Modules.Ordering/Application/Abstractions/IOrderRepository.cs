using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;
using PonPon.Modules.Ordering.Domain.Orders;

namespace PonPon.Modules.Ordering.Application.Abstractions;

public sealed record CustomerOrderListProjection(
    IReadOnlyList<CustomerOrderItem> Items,
    int Total);

public sealed record CustomerOrderItem(
    Order Order,
    int ItemsCount,
    IReadOnlyList<OrderItem> ItemsPreview);

public sealed record AdminOrderListItem(
    Order Order,
    string? ReturnRequestStatus);

public interface IOrderPaymentLock : IAsyncDisposable
{
    Task CompleteAsync(CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<IReadOnlyCollection<AdminOrderListItem>> GetAsync(
        string? keyword,
        string? status,
        string? paymentStatus,
        string? returnRequestStatus,
        string? refundRequestStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);
    Task<CustomerOrderListProjection> GetCustomerOrdersAsync(
        Guid customerId,
        IReadOnlyCollection<string>? statuses,
        IReadOnlyCollection<string>? paymentStatuses,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Order?> GetCustomerOrderByIdAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task<Order?> GetByZortOrderIdAsync(long zortOrderId, CancellationToken cancellationToken = default);
    Task<int> CountCustomerCompletedOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task ReloadAsync(Order order, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> GetPendingZortSyncAsync(int limit, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<bool> UpdatePaymentStatusAsync(
        Guid orderId,
        string expectedOmiseChargeId,
        string orderStatus,
        string paymentStatus,
        decimal paymentAmount,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> GetExpiredUnpaidAsync(DateTime now, string salesChannel, CancellationToken cancellationToken = default);
    Task<IOrderPaymentLock> AcquirePaymentLockAsync(Guid orderId, CancellationToken cancellationToken = default);
}
