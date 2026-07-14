using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.GetZortWebhookFromZort;

public sealed class GetZortWebhookFromZortHandler
{
    private readonly IZortWebhookRegistrar _registrar;

    public GetZortWebhookFromZortHandler(IZortWebhookRegistrar registrar)
    {
        _registrar = registrar;
    }

    public async Task<ZortWebhooksResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var info = await _registrar.GetAsync(cancellationToken);
        var webhooks = new List<ZortWebhookResponse>();

        Add(webhooks, "order.created", info.AddOrderUrl);
        Add(webhooks, "order.updated", info.UpdateOrderUrl);
        Add(webhooks, "order.deleted", info.DeleteOrderUrl);
        Add(webhooks, "order.tracking_updated", info.UpdateOrderTrackingUrl);
        Add(webhooks, "order.payment_updated", info.UpdateOrderPaymentUrl);
        Add(webhooks, "product.created", info.AddProductUrl);
        Add(webhooks, "product.updated", info.UpdateProductUrl);
        Add(webhooks, "product.deleted", info.DeleteProductUrl);
        Add(webhooks, "product.quantity_updated", info.UpdateQuantityUrl);

        return new ZortWebhooksResponse(webhooks);
    }

    private static void Add(List<ZortWebhookResponse> webhooks, string @event, string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            webhooks.Add(new ZortWebhookResponse(@event, url));
        }
    }
}

public sealed record ZortWebhooksResponse(IReadOnlyCollection<ZortWebhookResponse> Webhooks);

public sealed record ZortWebhookResponse(string Event, string Url);
