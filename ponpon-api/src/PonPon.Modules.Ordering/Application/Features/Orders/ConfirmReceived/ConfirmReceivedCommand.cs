namespace PonPon.Modules.Ordering.Application.Features.Orders.ConfirmReceived;

public sealed record ConfirmReceivedCommand(Guid OrderId, Guid CustomerId);

public sealed record ConfirmReceivedResponse(
    Guid Id,
    string Status,
    DateTime ReceivedAtUtc);
