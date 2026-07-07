namespace PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;

public sealed record CancelOrderRequest(string Reason);

public sealed record CancelOrderCommand(Guid OrderId, string Reason);
