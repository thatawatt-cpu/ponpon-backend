using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Payment.Application.Features.CreateCreditCardCharge;

public sealed class CreateCreditCardChargeHandler
{
    private const string Currency = "THB";

    private readonly IOmiseClient _omise;
    private readonly PaymentChargeCoordinator _coordinator;
    private readonly ICurrentUser _currentUser;
    private readonly IShopRealtimeNotificationService _shopRealtimeNotifications;

    public CreateCreditCardChargeHandler(
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

    public async Task<CreateCreditCardChargeResponse> HandleAsync(
        CreateCreditCardChargeCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CustomerId is not Guid customerId)
            throw new UnauthorizedException("Customer authentication is required.");

        var prepared = await _coordinator.PrepareAsync(
            command.OrderId,
            customerId,
            "card",
            cancellationToken);
        var result = prepared.ExistingCharge
            ?? await _omise.CreateCreditCardChargeAsync(
                command.TokenId,
                prepared.AmountSatang,
                Currency,
                prepared.Order.Number,
                command.ReturnUri,
                cancellationToken);

        if (prepared.ExistingCharge is null)
        {
            await _coordinator.CompleteAsync(
                prepared.Order.Id,
                customerId,
                "card",
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

        return new CreateCreditCardChargeResponse(
            result.ChargeId,
            result.Status,
            result.AuthorizeUri);
    }
}
