namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed record AddOrderRequest(
    Guid ClientRequestId,
    Guid QuoteId,
    string CustomerName,
    string? CustomerEmail,
    string CustomerPhone,
    string CustomerAddress,
    string ShippingName,
    string ShippingPhone,
    string ShippingAddress,
    string? ShippingChannel,
    decimal ShippingAmount,
    string? CouponCode,
    string? Description,
    IReadOnlyCollection<AddOrderItemRequest> Items,
    string? PaymentMethod = null,
    IReadOnlyCollection<string>? CouponCodes = null);

public sealed record AddOrderItemRequest(Guid ProductId, Guid? VariantId, int Quantity);
