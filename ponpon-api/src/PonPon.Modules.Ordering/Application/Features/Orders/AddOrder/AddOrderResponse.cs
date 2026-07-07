namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed record AddOrderResponse(
    Guid Id,
    long ZortOrderId,
    string Number,
    string Status,
    string PaymentStatus,
    decimal Amount,
    decimal ShippingAmount,
    decimal DiscountAmount,
    DateTime? PaymentExpiresAt);
