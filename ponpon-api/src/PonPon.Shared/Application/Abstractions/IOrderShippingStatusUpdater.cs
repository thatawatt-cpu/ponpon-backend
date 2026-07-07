namespace PonPon.Shared.Application.Abstractions;

public interface IOrderShippingStatusUpdater
{
    Task ApplyAsync(
        Guid orderId,
        ShippingOrderProgress progress,
        string? trackingNumber,
        DateTime? occurredAtUtc,
        CancellationToken cancellationToken = default);
}

public enum ShippingOrderProgress
{
    Shipping,
    Completed,
    Failed,
    Returned
}
