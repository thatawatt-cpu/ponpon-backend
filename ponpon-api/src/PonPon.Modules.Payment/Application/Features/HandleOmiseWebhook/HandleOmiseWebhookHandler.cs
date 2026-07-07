using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application.Features.HandleOmiseWebhook;

public sealed class HandleOmiseWebhookHandler
{

    private readonly IZortOrderClient _zort;
    private readonly IOrderRepository _orders;
    private readonly ILineOrderNotificationService _lineNotifications;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;
    private readonly ILogger<HandleOmiseWebhookHandler> _logger;
    private readonly IOmiseClient _omise;
    private readonly IOrderingUnitOfWork _unitOfWork;

    public HandleOmiseWebhookHandler(
        IZortOrderClient zort,
        IOrderRepository orders,
        ILineOrderNotificationService lineNotifications,
        IShopRealtimeNotificationService shopRealtimeNotifications,
        IOmiseClient omise,
        IOrderingUnitOfWork unitOfWork,
        ILogger<HandleOmiseWebhookHandler> logger)
    {
        _zort = zort;
        _orders = orders;
        _lineNotifications = lineNotifications;
        _shopRealtimeNotifications = shopRealtimeNotifications;
        _omise = omise;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(HandleOmiseWebhookCommand command, CancellationToken cancellationToken)
    {
        if (!command.Paid || command.ChargeStatus != "successful")
        {
            _logger.LogInformation(
                "Omise webhook ignored: ChargeId={ChargeId} Status={Status} Paid={Paid}",
                command.ChargeId, command.ChargeStatus, command.Paid);
            return;
        }

        var charge = await _omise.GetChargeAsync(command.ChargeId, cancellationToken);
        if (!charge.Paid
            || !string.Equals(charge.Status, "successful", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Omise webhook charge verification failed: ChargeId={ChargeId}",
                command.ChargeId);
            return;
        }

        var orderNumber = charge.Description?.Trim();
        if (string.IsNullOrEmpty(orderNumber))
        {
            _logger.LogWarning("Omise charge {ChargeId} has no description (order number missing)", command.ChargeId);
            return;
        }

        var order = await _orders.GetByNumberAsync(orderNumber, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Omise charge {ChargeId} references unknown order {OrderNumber}", charge.ChargeId, orderNumber);
            return;
        }

        await using var paymentLock = await _orders.AcquirePaymentLockAsync(order.Id, cancellationToken);
        await _orders.ReloadAsync(order, cancellationToken);

        if (!string.Equals(charge.Currency, "THB", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Omise charge currency does not match the order currency.");

        var expectedSatang = OrderPaymentSecurity.ToSatang(OrderPaymentAmountCalculator.Calculate(order));
        if (charge.Amount != expectedSatang)
        {
            throw new BadRequestException(
                $"Omise charge amount does not match the order amount. Expected {expectedSatang}, received {charge.Amount} satang.");
        }

        if (string.IsNullOrWhiteSpace(order.OmiseChargeId))
        {
            if (!order.IsPaymentCreationPending)
                throw new BadRequestException("Order has no pending payment creation for this Omise charge.");

            order.RegisterOmiseCharge(charge.ChargeId, DateTime.UtcNow);
            order.CompletePaymentCreation(DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (!string.Equals(order.OmiseChargeId, charge.ChargeId, StringComparison.Ordinal))
        {
            throw new BadRequestException("Omise charge does not belong to this order.");
        }
        else if (order.IsPaymentCreationPending)
        {
            order.CompletePaymentCreation(DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var amountBaht = charge.Amount / 100m;
        if ((string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
             || string.Equals(order.PaymentStatus, "1", StringComparison.OrdinalIgnoreCase))
            && order.PaymentAmount == amountBaht)
        {
            await paymentLock.CompleteAsync(cancellationToken);
            return;
        }

        var paymentMethod = charge.SourceType switch
        {
            "promptpay" => "QR Code",
            var t when t?.StartsWith("mobile_banking_") == true => "Mobile Banking",
            _ => "Credit Card"
        };

        // Step 2: record payment in Zort
        try
        {
            await _zort.UpdateOrderPaymentAsync(orderNumber, amountBaht, paymentMethod, command.PaidAt, cancellationToken);
            _logger.LogInformation(
                "Zort payment recorded: order={OrderNumber} amount={Amount} method={Method}",
                orderNumber, amountBaht, paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record payment in Zort for order {OrderNumber}", orderNumber);
        }

        // Step 3: move status to Waiting
        try
        {
            await _zort.UpdateOrderStatusAsync(orderNumber, (int)ZortOrderStatus.Waiting, cancellationToken);
            _logger.LogInformation("Zort order status â†’ Waiting: order={OrderNumber}", orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Zort order status for order {OrderNumber}", orderNumber);
        }

        // Update local DB
        var paymentUpdated = await _orders.UpdatePaymentStatusAsync(
            order.Id,
            charge.ChargeId,
            "Waiting",
            "Paid",
            amountBaht,
            cancellationToken);
        if (!paymentUpdated)
            throw new BadRequestException("Order payment state changed before the verified charge could be applied.");
        await paymentLock.CompleteAsync(cancellationToken);
        _logger.LogInformation("Local order {OrderNumber} marked as Waiting/Paid", orderNumber);

        {
            await _lineNotifications.NotifyPaymentSucceededAsync(
                new LineOrderNotification(
                    order.Id,
                    order.Number,
                    order.IntegrationCustomerId,
                    order.CustomerName,
                    amountBaht,
                    PaymentMethod: paymentMethod,
                    ShippingAddress: order.ShippingAddress ?? order.CustomerAddress),
                cancellationToken);
            await _shopRealtimeNotifications.NotifyAsync(
                new ShopRealtimeNotification(
                    order.CustomerId,
                    order.IntegrationCustomerId,
                    "payment_succeeded",
                    order.Id,
                    order.Number,
                    "ชำระเงินสำเร็จ",
                    "ร้านค้าจะเริ่มดำเนินการคำสั่งซื้อของคุณ",
                    amountBaht,
                    order.PaymentStatus),
                cancellationToken);
        }
    }
}
