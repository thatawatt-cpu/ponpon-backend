namespace PonPon.Modules.Ordering.Application.Features.Orders.CancelMyOrder;

public sealed record CancelMyOrderRequest(string Reason);

public sealed record CancelMyOrderCommand(Guid OrderId, Guid CustomerId, string Reason);
