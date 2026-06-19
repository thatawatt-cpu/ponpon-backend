using PonPon.Modules.Ordering.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed class GetOrdersHandler
{
    private readonly IOrderRepository _orders;

    public GetOrdersHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyCollection<OrderListItemResponse>> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var orders = await _orders.GetAsync(
            query.Keyword,
            query.Status,
            query.PaymentStatus,
            page,
            pageSize,
            cancellationToken);

        return orders.Select(x => new OrderListItemResponse(
            x.Id,
            x.ZortOrderId,
            x.Number,
            x.CustomerName,
            x.CustomerPhone,
            x.Status,
            x.PaymentStatus,
            x.Amount,
            x.PaymentAmount,
            x.ShippingChannel,
            x.TrackingNo,
            x.OrderDate,
            x.SalesChannel,
            x.LastSyncedAt)).ToArray();
    }
}
