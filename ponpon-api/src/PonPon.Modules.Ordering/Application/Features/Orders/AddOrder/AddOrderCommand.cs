namespace PonPon.Modules.Ordering.Application.Features.Orders.AddOrder;

public sealed record AddOrderCommand(
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
    IReadOnlyCollection<AddOrderItemCommand> Items);

public sealed record AddOrderItemCommand(Guid ProductId, Guid? VariantId, int Quantity);
