using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;

public sealed class GetMyOrderReturnRequestHandler
{
    private readonly IOrderRepository _orders;
    private readonly IOrderReturnRequestRepository _returnRequests;

    public GetMyOrderReturnRequestHandler(
        IOrderRepository orders,
        IOrderReturnRequestRepository returnRequests)
    {
        _orders = orders;
        _returnRequests = returnRequests;
    }

    public async Task<OrderReturnRequestResponse> HandleAsync(
        GetMyOrderReturnRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = await _orders.GetCustomerOrderByIdAsync(
            query.OrderId,
            query.CustomerId,
            cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        var request = await _returnRequests.GetByOrderIdAsync(query.OrderId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        return CreateOrderReturnRequestHandler.ToResponse(request);
    }
}

public sealed class GetOrderReturnRequestHandler
{
    private readonly IOrderReturnRequestRepository _returnRequests;

    public GetOrderReturnRequestHandler(IOrderReturnRequestRepository returnRequests)
    {
        _returnRequests = returnRequests;
    }

    public async Task<OrderReturnRequestResponse?> HandleAsync(
        GetOrderReturnRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        var request = await _returnRequests.GetByOrderIdAsync(query.OrderId, cancellationToken);
        return request is null
            ? null
            : CreateOrderReturnRequestHandler.ToResponse(request);
    }
}
