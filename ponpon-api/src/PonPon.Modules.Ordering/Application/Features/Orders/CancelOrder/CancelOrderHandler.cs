using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CancelOrderHandler(
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

    public async Task HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        await _zortClient.VoidOrderAsync(order.ZortOrderId, cancellationToken);

        var zortOrder = await _zortClient.GetOrderDetailAsync(order.ZortOrderId, cancellationToken);
        var snapshot = ZortOrderMapper.ToSnapshot(zortOrder);
        order.ApplyZortSnapshot(snapshot, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
