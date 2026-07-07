namespace PonPon.Shared.Application.Abstractions;

public interface IOrderPaymentRefundService
{
    Task RefundOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}
