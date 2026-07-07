using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Settings.Application.Features.GetZortWebhookFromZort;

public sealed class GetZortWebhookFromZortHandler
{
    private readonly IZortWebhookRegistrar _registrar;

    public GetZortWebhookFromZortHandler(IZortWebhookRegistrar registrar)
    {
        _registrar = registrar;
    }

    public Task<ZortWebhookInfo> HandleAsync(CancellationToken cancellationToken = default)
        => _registrar.GetAsync(cancellationToken);
}
