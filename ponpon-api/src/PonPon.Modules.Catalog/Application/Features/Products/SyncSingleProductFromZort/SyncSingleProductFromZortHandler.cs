namespace PonPon.Modules.Catalog.Application.Features.Products.SyncSingleProductFromZort;

public sealed class SyncSingleProductFromZortHandler
{
    public Task HandleAsync(SyncSingleProductFromZortCommand command, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Single product sync will be added when the ZORT single-product endpoint is enabled.");
    }
}
