using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;

namespace PonPon.Modules.Ordering.Application.Abstractions;

public interface IZortOrderClient
{
    Task<long> AddOrderAsync(ZortAddOrderRequest request, CancellationToken cancellationToken = default);
    Task<ZortOrderDto> GetOrderDetailAsync(long zortOrderId, CancellationToken cancellationToken = default);
    Task<ZortGetOrdersResponse> GetOrdersAsync(
        int page,
        int limit,
        string salesChannel,
        CancellationToken cancellationToken = default);
    Task VoidOrderAsync(long zortOrderId, CancellationToken cancellationToken = default);
    Task UpdateOrderPaymentAsync(string orderNumber, decimal amount, string paymentMethod, DateTime? paidAt, CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(string orderNumber, int status, CancellationToken cancellationToken = default);
    Task RegisterWebhookAsync(string updateOrderUrl, string key1, string? key2 = null, string? key3 = null, CancellationToken cancellationToken = default);
    Task<ZortGetWebhookResponse> GetWebhookAsync(CancellationToken cancellationToken = default);
}
