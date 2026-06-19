using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;

public sealed class SyncOrdersFromZortHandler
{
    public const string LineLiffSalesChannel = "LineLiff";

    private readonly IZortOrderClient _zortClient;
    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SyncOrdersFromZortHandler(
        IZortOrderClient zortClient,
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _zortClient = zortClient;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<SyncOrdersFromZortResponse> HandleAsync(
        SyncOrdersFromZortCommand command,
        CancellationToken cancellationToken = default)
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

        return new SyncOrdersFromZortResponse(totalFetched, created, updated, failed, errors);
    }
}
