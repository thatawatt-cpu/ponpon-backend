using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Infrastructure;

public sealed class ZortWebhookRegistrar : IZortWebhookRegistrar
{
    private readonly IZortOrderClient _zort;

    public ZortWebhookRegistrar(IZortOrderClient zort)
    {
        _zort = zort;
    }

    public Task RegisterAsync(string baseUrl, string key1, string? key2 = null, string? key3 = null, CancellationToken cancellationToken = default)
    {
        var b = baseUrl.TrimEnd('/');
        return _zort.RegisterWebhookAsync(
            $"{b}/api/webhooks/zort/order",
            key1, key2, key3, cancellationToken);
    }

    public async Task<ZortWebhookInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        var r = await _zort.GetWebhookAsync(cancellationToken);
        return new ZortWebhookInfo(
            r.AddOrderUrl, r.UpdateOrderUrl, r.DeleteOrderUrl,
            r.UpdateOrderTrackingUrl, r.UpdateOrderPaymentUrl,
            r.AddProductUrl, r.UpdateProductUrl, r.DeleteProductUrl,
            r.UpdateQuantityUrl,
            r.Key1, r.Key2, r.Key3);
    }
}
