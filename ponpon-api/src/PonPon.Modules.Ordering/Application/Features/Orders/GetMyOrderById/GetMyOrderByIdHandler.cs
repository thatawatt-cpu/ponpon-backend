using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;

public sealed class GetMyOrderByIdHandler
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUser _currentUser;

    public GetMyOrderByIdHandler(IOrderRepository orders, ICurrentUser currentUser)
    {
        _orders = orders;
        _currentUser = currentUser;
    }

    public async Task<MyOrderDetailResponse> HandleAsync(
        GetMyOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var order = await _orders.GetCustomerOrderByIdAsync(query.Id, customerId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        return new MyOrderDetailResponse(
            order.Id,
            order.Number,
            order.Status,
            order.PaymentStatus,
            order.Amount,
            order.VatAmount,
            order.ShippingAmount,
            order.PaymentAmount,
            order.DiscountAmount,
            order.ShippingChannel,
            order.ShippingName,
            order.ShippingAddress,
            order.ShippingPhone,
            order.TrackingNo,
            order.OrderDate,
            order.ShippingDate,
            order.Reference,
            order.Description,
            order.IsCod,
            order.Currency,
            order.Items.Select(x => new MyOrderItemResponse(
                x.Id,
                x.Sku,
                x.Name,
                x.Quantity,
                x.UnitText,
                x.PricePerUnit,
                x.Discount,
                x.DiscountAmount,
                x.TotalPrice)).ToArray(),
            order.Payments.Select(x => new MyOrderPaymentResponse(
                x.Id,
                x.Name,
                x.Amount,
                x.PaymentDateTime)).ToArray());
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
