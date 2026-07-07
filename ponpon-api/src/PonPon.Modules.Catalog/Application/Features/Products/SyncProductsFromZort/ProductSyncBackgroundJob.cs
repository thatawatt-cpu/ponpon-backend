namespace PonPon.Modules.Catalog.Application.Features.Products.SyncProductsFromZort;

public sealed class ProductSyncBackgroundJob
{
    private readonly SyncProductsFromZortHandler _handler;

    public ProductSyncBackgroundJob(SyncProductsFromZortHandler handler)
    {
        _handler = handler;
    }

    public Task ExecuteAsync(
        Guid syncRunId,
        int pageStart,
        int pageLimit,
        int? maxPages,
        bool deactivateMissingProducts)
    {
        return _handler.HandleAsync(
            new SyncProductsFromZortCommand(
                pageStart,
                pageLimit,
                maxPages,
                deactivateMissingProducts,
                syncRunId),
            CancellationToken.None);
    }
}
