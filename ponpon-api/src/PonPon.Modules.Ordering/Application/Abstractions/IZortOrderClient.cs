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
}
