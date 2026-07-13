using PonPon.Modules.Ordering.Application;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ConfirmReceived;

public sealed class ConfirmReceivedHandler
{
    private readonly IOrderRepository _orders;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ConfirmReceivedHandler(
        IOrderRepository orders,
        IOrderingUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ConfirmReceivedResponse> HandleAsync(
        ConfirmReceivedCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetCustomerOrderByIdAsync(
            command.OrderId,
            command.CustomerId,
            cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        if (!CanConfirmReceived(order.Status))
        {
            throw new BadRequestException("Order can be confirmed received only after it has shipped or been delivered.");
        }

        var now = _clock.UtcNow;
        order.MarkReceived(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmReceivedResponse(order.Id, order.Status, order.ReceivedAtUtc.GetValueOrDefault(now));
    }

    private static bool CanConfirmReceived(string status)
    {
        return string.Equals(status, ZortOrderStatus.Shipping.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ((int)ZortOrderStatus.Shipping).ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ZortOrderStatus.Success.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ((int)ZortOrderStatus.Success).ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
