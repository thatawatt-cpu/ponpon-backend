using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Modules.Ordering.Application.Services;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zortClient;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IShippingBookingAutomation _shippingBooking;
    private readonly IOrderPaymentRefundService _paymentRefund;
    private readonly IDateTimeProvider _clock;
    private readonly OrderStockReservationService _stockReservations;

    public CancelOrderHandler(
        IOrderRepository orders,
        IZortOrderClient zortClient,
        IOrderingUnitOfWork unitOfWork,
        IShippingBookingAutomation shippingBooking,
        IOrderPaymentRefundService paymentRefund,
        OrderStockReservationService stockReservations,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _zortClient = zortClient;
        _unitOfWork = unitOfWork;
        _shippingBooking = shippingBooking;
        _paymentRefund = paymentRefund;
        _stockReservations = stockReservations;
        _clock = clock;
    }

    public async Task HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        ValidateReason(command.Reason);

        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase))
            return;

        await _shippingBooking.CancelBookingForOrderAsync(
            order.Id,
            command.Reason,
            cancellationToken);

        await _paymentRefund.RefundOrderAsync(order.Id, cancellationToken);
        if (order.ZortOrderId > 0)
        {
            await _zortClient.VoidOrderAsync(order.ZortOrderId, cancellationToken);
        }

        order.MarkVoided(_clock.UtcNow, command.Reason, "Admin");
        await _stockReservations.ReleaseAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000)
        {
            throw new BadRequestException("Cancellation reason is required and must not exceed 2000 characters.");
        }
    }
}
