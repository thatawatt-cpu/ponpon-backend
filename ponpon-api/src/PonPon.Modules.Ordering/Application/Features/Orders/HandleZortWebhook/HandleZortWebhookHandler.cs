using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.HandleZortWebhook;

public sealed class HandleZortWebhookHandler
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public HandleZortWebhookHandler(
        IOrderRepository orders,
        IZortOrderClient zortClient,
        IOrderingUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _zortClient = zortClient;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(HandleZortWebhookCommand command, CancellationToken cancellationToken = default)
    {
        var zortOrder = await _zortClient.GetOrderDetailAsync(command.ZortOrderId, cancellationToken);
        var snapshot = ZortOrderMapper.ToSnapshot(zortOrder);

        var order = await _orders.GetByZortOrderIdAsync(snapshot.ZortOrderId, cancellationToken);
        if (order is null)
        {
            order = Order.CreateFromZort(snapshot, _clock.UtcNow);
            await _orders.AddAsync(order, cancellationToken);
        }
        else
        {
            order.ApplyZortSnapshot(snapshot, _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
