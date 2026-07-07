using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application.Features.CreatePromptPayCharge;

public sealed class CreatePromptPayChargeHandler
{
    private const string Currency = "THB";

    private readonly IOmiseClient _omise;
    private readonly PaymentChargeCoordinator _coordinator;
    private readonly ICurrentUser _currentUser;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;

    public CreatePromptPayChargeHandler(
        IOmiseClient omise,
        PaymentChargeCoordinator coordinator,
        ICurrentUser currentUser,
        IShopRealtimeNotificationService shopRealtimeNotifications)
    {
        _omise = omise;
        _coordinator = coordinator;
        _currentUser = currentUser;
        _shopRealtimeNotifications = shopRealtimeNotifications;
    }

    public async Task<CreatePromptPayChargeResponse> HandleAsync(
        CreatePromptPayChargeCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CustomerId is not Guid customerId)
            throw new UnauthorizedException("Customer authentication is required.");

        var prepared = await _coordinator.PrepareAsync(
            command.OrderId,
            customerId,
            "promptpay",
            cancellationToken);
        var result = prepared.ExistingCharge
            ?? await _omise.CreatePromptPayChargeAsync(
                prepared.AmountSatang,
                Currency,
                prepared.Order.Number,
                cancellationToken);

        if (result.QrCodeUrl is null)
            throw new InvalidOperationException("Omise did not return a QR code URL.");

        if (prepared.ExistingCharge is null)
        {
            await _coordinator.CompleteAsync(
                prepared.Order.Id,
                customerId,
                "promptpay",
                prepared.AmountSatang,
                result,
                cancellationToken);
        }

        await _shopRealtimeNotifications.NotifyAsync(
            new ShopRealtimeNotification(
                prepared.Order.CustomerId,
                prepared.Order.IntegrationCustomerId,
                "payment_created",
                prepared.Order.Id,
                prepared.Order.Number,
                "พร้อมชำระเงิน",
                "ระบบสร้างรายการชำระเงินแล้ว",
                prepared.AmountBaht,
                result.Status),
            cancellationToken);

        return new CreatePromptPayChargeResponse(
            result.ChargeId,
            result.QrCodeUrl,
            result.Amount,
            result.Currency,
            result.ExpiresAt);
    }
}
