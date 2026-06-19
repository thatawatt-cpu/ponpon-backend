namespace PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;

public sealed record CancelMyOrderCommand(Guid OrderId, Guid CustomerId);
