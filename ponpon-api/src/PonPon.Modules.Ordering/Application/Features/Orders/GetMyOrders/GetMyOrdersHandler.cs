using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;

public sealed class GetMyOrdersHandler
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUser _currentUser;

    public GetMyOrdersHandler(IOrderRepository orders, ICurrentUser currentUser)
    {
        _orders = orders;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyCollection<MyOrderListItemResponse>> HandleAsync(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var orders = await _orders.GetCustomerOrdersAsync(
            customerId,
            query.Status,
            query.PaymentStatus,
            Math.Max(query.Page, 1),
            Math.Clamp(query.PageSize, 1, 100),
            cancellationToken);

        return orders.Select(x => new MyOrderListItemResponse(
            x.Id,
            x.Number,
            x.Status,
            x.PaymentStatus,
            x.Amount,
            x.PaymentAmount,
            x.ShippingChannel,
            x.TrackingNo,
            x.OrderDate)).ToArray();
    }

    private Guid GetCustomerId()
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserType != "Customer"
            || _currentUser.CustomerId is not Guid customerId)
        {
            throw new UnauthorizedException("Customer authentication is required.");
        }

        return customerId;
    }
}
