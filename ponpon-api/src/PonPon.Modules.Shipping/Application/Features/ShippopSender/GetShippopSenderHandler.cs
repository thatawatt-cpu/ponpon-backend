using PonPon.Modules.Shipping.Application.Abstractions;

namespace PonPon.Modules.Shipping.Application.Features.ShippopSender;

public sealed class GetShippopSenderHandler
{
    private readonly IShippopSenderRepository _senders;

    public GetShippopSenderHandler(IShippopSenderRepository senders)
    {
        _senders = senders;
    }

    public async Task<ShippopSenderResponse> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var sender = await _senders.GetAsync(cancellationToken);
        return sender is null
            ? ShippopSenderResponse.Empty()
            : ShippopSenderResponse.From(sender);
    }
}
