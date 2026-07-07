using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.SyncRuns;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Services;

namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;

public sealed class SyncOrdersFromZortHandler
{
    public const string LineLiffSalesChannel = "LineLiff";

    private readonly IZortOrderClient _zortClient;
    private readonly IOrderRepository _orders;
    private readonly IOrderSyncRunRepository _syncRuns;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly OrderStockReservationService _stockReservations;

    public SyncOrdersFromZortHandler(
        IZortOrderClient zortClient,
        IOrderRepository orders,
        IOrderSyncRunRepository syncRuns,
        IOrderingUnitOfWork unitOfWork,
        OrderStockReservationService stockReservations,
        IDateTimeProvider clock)
    {
        _zortClient = zortClient;
        _orders = orders;
        _syncRuns = syncRuns;
        _unitOfWork = unitOfWork;
        _stockReservations = stockReservations;
        _clock = clock;
    }

    public async Task<SyncOrdersFromZortResponse> HandleAsync(
        SyncOrdersFromZortCommand command,
        CancellationToken cancellationToken = default)
    {
        var syncRun = command.SyncRunId.HasValue
            ? await _syncRuns.GetByIdAsync(command.SyncRunId.Value, cancellationToken)
            : null;

        if (syncRun is null)
        {
            syncRun = OrderSyncRun.Queue(_clock.UtcNow);
            await _syncRuns.AddAsync(syncRun, cancellationToken);
        }

        syncRun.MarkRunning(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await ExecuteAsync(syncRun.Id, command, cancellationToken);
            syncRun.MarkCompleted(
                result.TotalFetched,
                result.Created,
                result.Updated,
                result.Failed,
                result.Errors,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            syncRun.MarkFailed(ex.Message, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<SyncOrdersFromZortResponse> ExecuteAsync(
        Guid syncRunId,
        SyncOrdersFromZortCommand command,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(command.PageStart, 1);
        var pageLimit = Math.Clamp(command.PageLimit, 1, 500);
        var pagesProcessed = 0;
        var totalFetched = 0;
        var created = 0;
        var updated = 0;
        var failed = 0;
        var errors = new List<string>();

        while (command.MaxPages is null || pagesProcessed < command.MaxPages.Value)
        {
            var response = await _zortClient.GetOrdersAsync(
                page,
                pageLimit,
                LineLiffSalesChannel,
                cancellationToken);

            if (response.Orders.Count == 0)
            {
                break;
            }

            foreach (var zortOrder in response.Orders)
            {
                try
                {
                    var snapshot = ZortOrderMapper.ToSnapshot(zortOrder);
                    var order = await _orders.GetByZortOrderIdAsync(snapshot.ZortOrderId, cancellationToken);
                    if (order is null)
                    {
                        order = Order.CreateFromZort(snapshot, _clock.UtcNow);
                        await _orders.AddAsync(order, cancellationToken);
                        created++;
                    }
                    else
                    {
                        order.ApplyZortSnapshot(snapshot, _clock.UtcNow);
                        if (IsVoided(order.Status))
                            await _stockReservations.ReleaseAsync(order, cancellationToken);
                        updated++;
                    }

                    totalFetched++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add(ex.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            pagesProcessed++;
            page++;

            if (response.Orders.Count < pageLimit)
            {
                break;
            }
        }

        return new SyncOrdersFromZortResponse(syncRunId, totalFetched, created, updated, failed, errors);
    }

    private static bool IsVoided(string status)
        => string.Equals(status, ZortOrderStatus.Voided.ToString(), StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, ((int)ZortOrderStatus.Voided).ToString(), StringComparison.OrdinalIgnoreCase);
}
