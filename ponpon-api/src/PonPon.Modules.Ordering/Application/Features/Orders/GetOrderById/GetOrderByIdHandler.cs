using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdHandler
{
    private readonly IOrderRepository _orders;

    public GetOrderByIdHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<OrderDetailResponse> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        return new OrderDetailResponse(
            order.Id,
            order.ZortOrderId,
            order.Number,
            order.ZortCustomerId,
            order.CustomerCode,
            order.CustomerName,
            order.CustomerIdNumber,
            order.CustomerEmail,
            order.CustomerPhone,
            order.CustomerAddress,
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
            order.SalesChannel,
            order.IntegrationCustomerId,
            order.IntegrationCustomer,
            order.WarehouseCode,
            order.IsCod,
            order.Currency,
            order.ZortCreatedAt,
            order.ZortUpdatedAt,
            order.LastSyncedAt,
            order.Items.Select(x => new OrderItemResponse(
                x.Id,
                x.ZortProductId,
                x.Sku,
                x.Name,
                x.Quantity,
                x.UnitText,
                x.PricePerUnit,
                x.Discount,
                x.DiscountAmount,
                x.TotalPrice,
                x.ProductType,
                x.BundleId,
                x.BundleCode,
                x.BundleName)).ToArray(),
            order.Payments.Select(x => new OrderPaymentResponse(
                x.Id,
                x.ZortPaymentId,
                x.Name,
                x.Amount,
                x.PaymentDateTime)).ToArray());
    }
}
