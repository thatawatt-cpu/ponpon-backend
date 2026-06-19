namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed record AddOrderRequest(
    Guid ClientRequestId,
    string CustomerName,
    string? CustomerEmail,
    string CustomerPhone,
    string CustomerAddress,
    string ShippingName,
    string ShippingPhone,
    string ShippingAddress,
    string? ShippingChannel,
    decimal ShippingAmount,
    string? Description,
    IReadOnlyCollection<AddOrderItemRequest> Items);

public sealed record AddOrderItemRequest(Guid ProductId, Guid? VariantId, int Quantity);
