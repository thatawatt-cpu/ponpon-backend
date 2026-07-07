using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.ReturnOrder;

public sealed class UpdateOrderReturnRequestHandler
{
    private readonly IOrderRepository _orders;
    private readonly IOrderReturnRequestRepository _returnRequests;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly ILineOrderNotificationService _lineNotifications;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly IDateTimeProvider _clock;

    public UpdateOrderReturnRequestHandler(
        IOrderRepository orders,
        IOrderReturnRequestRepository returnRequests,
        IOrderingUnitOfWork unitOfWork,
        ILineOrderNotificationService lineNotifications,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _returnRequests = returnRequests;
        _unitOfWork = unitOfWork;
        _lineNotifications = lineNotifications;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _clock = clock;
    }

    public async Task<OrderReturnRequestResponse> HandleAsync(
        UpdateOrderReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = OrderReturnRequestStatus.NormalizeAdminStatus(
            command.Status?.Trim() ?? string.Empty);
        if (normalizedStatus is null)
        {
            throw new BadRequestException("Return status must be Approved, Rejected, or Completed.");
        }

        if (command.AdminNote?.Length > 2000)
        {
            throw new BadRequestException("Admin note must not exceed 2000 characters.");
        }

        var request = await _returnRequests.GetByOrderIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        var previousStatus = request.Status;

        var order = await _orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        request.UpdateStatus(normalizedStatus, command.AdminNote, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.Equals(previousStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(normalizedStatus, OrderReturnRequestStatus.Completed, StringComparison.OrdinalIgnoreCase))
            {
                await _lineNotifications.NotifyReturnRefundCompletedAsync(
                    new LineOrderNotification(
                        order.Id,
                        order.Number,
                        order.IntegrationCustomerId,
                        order.CustomerName,
                        order.PaymentAmount,
                        Status: normalizedStatus,
                        Reason: command.AdminNote),
                    cancellationToken);
                await _shopRealtimeNotifications.NotifyAsync(
                    CreateShopNotification(
                        order,
                        "return_refund_completed",
                        "คืนเงินเรียบร้อยแล้ว",
                        "ร้านค้าดำเนินการคืนเงินและปิดรายการคืนสินค้าเรียบร้อยแล้ว",
                        normalizedStatus),
                    cancellationToken);
            }
            else
            {
                await _shopRealtimeNotifications.NotifyAsync(
                    CreateShopNotification(
                        order,
                        "return_request_updated",
                        "อัปเดตคำขอคืนสินค้า",
                        $"สถานะคำขอคืนสินค้า: {normalizedStatus}",
                        normalizedStatus),
                    cancellationToken);
            }
        }

        return CreateOrderReturnRequestHandler.ToResponse(request);
    }

    private static ShopRealtimeNotification CreateShopNotification(
        Order order,
        string type,
        string title,
        string message,
        string status)
        => new(
            order.CustomerId,
            order.IntegrationCustomerId,
            type,
            order.Id,
            order.Number,
            title,
            message,
            order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount,
            status);
}
