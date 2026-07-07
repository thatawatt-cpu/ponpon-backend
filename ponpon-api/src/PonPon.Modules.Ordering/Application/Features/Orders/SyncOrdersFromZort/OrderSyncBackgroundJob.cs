namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;

public sealed class OrderSyncBackgroundJob
{
    private readonly SyncOrdersFromZortHandler _handler;

    public OrderSyncBackgroundJob(SyncOrdersFromZortHandler handler)
    {
        _handler = handler;
    }

    public Task ExecuteAsync(
        Guid syncRunId,
        int pageStart,
        int pageLimit,
        int? maxPages)
    {
        return _handler.HandleAsync(
            new SyncOrdersFromZortCommand(
                pageStart,
                pageLimit,
                maxPages,
                syncRunId),
            CancellationToken.None);
    }
}
