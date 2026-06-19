namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;

public sealed record MyOrderListItemResponse(
    Guid Id,
    string Number,
    string Status,
    string PaymentStatus,
    decimal Amount,
    decimal PaymentAmount,
    string? ShippingChannel,
    string? TrackingNo,
    DateTime? OrderDate);
